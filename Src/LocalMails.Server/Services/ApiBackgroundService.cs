using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using LocalMails.Server.Data;
using LocalMails.Server.Models;
using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace LocalMails.Server.Services;

public class ApiBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ApiBackgroundService> _logger;
    private readonly IEmailNotifier _notifier;
    private HttpListener? _listener;

    public ApiBackgroundService(IServiceProvider serviceProvider, ILogger<ApiBackgroundService> logger, IEmailNotifier notifier)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _notifier = notifier;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        int apiPort = 8025;
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{apiPort}/api/v1/messages/");
        _listener.Prefixes.Add($"http://127.0.0.1:{apiPort}/api/v1/messages/");

        try
        {
            _listener.Start();
            _logger.LogInformation($"Integration API started on port {apiPort}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start HTTP API listener. Maybe port is in use or requires Admin privileges.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleRequestAsync(context, stoppingToken), stoppingToken);
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 995 || _listener.IsListening == false)
            {
                // Listener was stopped or disposed.
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API listener error.");
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var response = context.Response;
        var request = context.Request;
        
        response.ContentType = "application/json";
        response.Headers.Add("Access-Control-Allow-Origin", "*");
        
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (request.HttpMethod == "GET")
            {
                var search = request.QueryString["search"]?.ToLower();
                var query = db.Messages.AsQueryable();

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(m => 
                        m.Subject.ToLower().Contains(search) || 
                        m.To.ToLower().Contains(search) || 
                        m.From.ToLower().Contains(search));
                }

                var messages = await query.OrderByDescending(m => m.ReceivedDate).Take(50).ToListAsync(cancellationToken);
                var json = JsonSerializer.Serialize(new { total = messages.Count, messages = messages });
                
                var buffer = System.Text.Encoding.UTF8.GetBytes(json);
                response.StatusCode = 200;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
            }
            else if (request.HttpMethod == "DELETE")
            {
                var allMessages = await db.Messages.ToListAsync(cancellationToken);
                db.Messages.RemoveRange(allMessages);
                await db.SaveChangesAsync(cancellationToken);
                
                // Keep the UI synchronized when external API clears emails
                _notifier.NotifyEmailsCleared();
                
                response.StatusCode = 200;
                var buffer = System.Text.Encoding.UTF8.GetBytes("{\"status\":\"ok\"}");
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
            }
            else
            {
                response.StatusCode = 405; // Method Not Allowed
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle API request.");
            response.StatusCode = 500;
        }
        finally
        {
            response.Close();
        }
    }

    public override void Dispose()
    {
        try { _listener?.Stop(); } catch { }
        try { _listener?.Close(); } catch { }
        base.Dispose();
    }
}
