using LocalMails.Server.Models;
using LocalMails.Server.Services;
using LocalMails.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LocalMails.Shared.Services;

public class MailState : IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEmailNotifier _notifier;
    
    public List<EmailMessage> Messages { get; private set; } = new();
    private string _searchTerm = "";
    public string SearchTerm 
    { 
        get => _searchTerm; 
        set { if (_searchTerm != value) { _searchTerm = value; NotifyChanged(); } } 
    }

    private EmailMessage? _selectedMessage;
    public EmailMessage? SelectedMessage 
    { 
        get => _selectedMessage; 
        set { if (_selectedMessage != value) { _selectedMessage = value; NotifyChanged(); } } 
    }
    
    public event Action? OnChanged;
    
    public MailState(IServiceScopeFactory scopeFactory, IEmailNotifier notifier)
    {
        _scopeFactory = scopeFactory;
        _notifier = notifier;
        
        _notifier.OnEmailReceived += HandleNewEmail;
        _notifier.OnEmailsCleared += HandleEmailsCleared;
    }
    
    private readonly object _lock = new();

    public async Task LoadMessagesAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var items = await db.Messages.OrderByDescending(m => m.ReceivedDate).Take(100).ToListAsync();
        lock (_lock)
        {
            Messages = items;
        }
        NotifyChanged();
    }
    
    public async Task ClearInboxAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Messages.RemoveRange(db.Messages);
        await db.SaveChangesAsync();
        
        lock (_lock)
        {
            Messages.Clear();
            _selectedMessage = null;
        }
        NotifyChanged();
    }
    
    private void HandleNewEmail(EmailMessage msg)
    {
        lock (_lock)
        {
            Messages.Insert(0, msg);
        }
        NotifyChanged();
    }
    
    private void HandleEmailsCleared()
    {
        lock (_lock)
        {
            Messages.Clear();
            _selectedMessage = null;
        }
        NotifyChanged();
    }
    
    private void NotifyChanged() => OnChanged?.Invoke();
    
    public void Dispose()
    {
        _notifier.OnEmailReceived -= HandleNewEmail;
        _notifier.OnEmailsCleared -= HandleEmailsCleared;
    }
}
