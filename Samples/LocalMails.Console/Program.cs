using System.Net.Mail;
using System.Net;

Console.WriteLine("LocalMails - Test SMTP Client");
Console.WriteLine("=============================");

Console.Write("Enter number of test emails to send (default 1): ");
var input = Console.ReadLine();
int count = int.TryParse(input, out int parsed) ? parsed : 1;

try
{
    using var client = new SmtpClient("127.0.0.1", 1025);
    
    // No SSL/TLS for local testing
    client.EnableSsl = false;
    
    // LocalMails does not require authentication
    client.Credentials = CredentialCache.DefaultNetworkCredentials;

    for (int i = 1; i <= count; i++)
    {
        var message = new MailMessage
        {
            From = new MailAddress($"sender{i}@example.com", "LocalMails Sender"),
            Subject = $"Test Email #{i} - {DateTime.Now:HH:mm:ss}",
            Body = $"<h1>Hello from Console App!</h1><p>This is a test email <strong>#{i}</strong> sent to LocalMails SMTP server to verify that everything works correctly.</p><br/><ul><li>HTML Support: Yes</li><li>Fast Delivery: Yes</li></ul>",
            IsBodyHtml = true
        };
        
        message.To.Add(new MailAddress($"recipient{i}@test.local", "Test Recipient"));
        
        client.Send(message);
        Console.WriteLine($"[✓] Sent email #{i}");
    }
    
    Console.WriteLine("\nAll test emails sent successfully!");
    Console.WriteLine("Check your LocalMails App inbox to see them.");
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\n[Error] Failed to send email: {ex.Message}");
    if (ex.InnerException != null)
    {
        Console.WriteLine($"   * Inner Error: {ex.InnerException.Message}");
    }
    Console.ResetColor();
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();
