using LocalMails.Server.Models;

namespace LocalMails.Server.Services;

public interface IEmailNotifier
{
    event Action<EmailMessage> OnEmailReceived;
    event Action OnEmailsCleared;
    void NotifyEmailReceived(EmailMessage message);
    void NotifyEmailsCleared();
}

public class EmailNotifier : IEmailNotifier
{
    public event Action<EmailMessage>? OnEmailReceived;
    public event Action? OnEmailsCleared;

    public void NotifyEmailReceived(EmailMessage message)
    {
        OnEmailReceived?.Invoke(message);
    }

    public void NotifyEmailsCleared()
    {
        OnEmailsCleared?.Invoke();
    }
}
