using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SmtpServer;
using SmtpServer.ComponentModel;

namespace LocalMails.Server.Services;

public class SmtpBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SmtpBackgroundService> _logger;
    private SmtpServer.SmtpServer? _smtpServer;

    public SmtpBackgroundService(IServiceProvider serviceProvider, ILogger<SmtpBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // In a real app, port could be retrieved from AppSettings
        int port = 1025; 

        var options = new SmtpServerOptionsBuilder()
            .ServerName("localhost")
            .Port(port)
            .Build();

        _smtpServer = new SmtpServer.SmtpServer(options, _serviceProvider);

        _logger.LogInformation($"SMTP Server starting on port {port}");
        
        try 
        {
            await _smtpServer.StartAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SMTP Server cancellation requested.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error running SMTP server.");
        }
    }
}
