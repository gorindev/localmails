using LocalMails.Server.Data;
using LocalMails.Server.Models;
using Microsoft.Extensions.DependencyInjection;
using MimeKit;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using System.Buffers;

namespace LocalMails.Server.Services;

public class LocalMailsMessageStore : MessageStore
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEmailNotifier _notifier;

    public LocalMailsMessageStore(IServiceProvider serviceProvider, IEmailNotifier notifier)
    {
        _serviceProvider = serviceProvider;
        _notifier = notifier;
    }

    public override async Task<SmtpResponse> SaveAsync(
        ISessionContext context, 
        IMessageTransaction transaction, 
        ReadOnlySequence<byte> buffer, 
        CancellationToken cancellationToken)
    {
        // Parse the Mime message using MimeKit
        await using var stream = new MemoryStream(buffer.ToArray());
        var mimeMessage = await MimeMessage.LoadAsync(stream, cancellationToken);

        var msg = new EmailMessage
        {
            Id = Guid.NewGuid().ToString(),
            Subject = mimeMessage.Subject ?? "(No Subject)",
            From = mimeMessage.From.ToString(),
            To = mimeMessage.To.ToString(),
            Cc = mimeMessage.Cc.ToString(),
            Bcc = mimeMessage.Bcc.ToString(),
            ReceivedDate = DateTime.UtcNow,
            BodyHtml = mimeMessage.HtmlBody ?? string.Empty,
            BodyText = mimeMessage.TextBody ?? string.Empty,
            Size = buffer.Length
        };

        // If body text and HTML are both empty, format simple body from text content.
        if (string.IsNullOrEmpty(msg.BodyText) && string.IsNullOrEmpty(msg.BodyHtml))
        {
            msg.BodyText = mimeMessage.TextBody ?? "";
        }

        // Save to DB using scoped DbContext
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Messages.Add(msg);
        await db.SaveChangesAsync(cancellationToken);

        // Notify UI components globally
        _notifier.NotifyEmailReceived(msg);

        return SmtpResponse.Ok;
    }
}
