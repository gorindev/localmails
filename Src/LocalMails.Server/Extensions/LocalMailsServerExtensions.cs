using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using LocalMails.Server.Data;
using LocalMails.Server.Services;
using SmtpServer.Storage;

namespace LocalMails.Server.Extensions;

public static class LocalMailsServerExtensions
{
    public static IServiceCollection AddLocalMailsServer(this IServiceCollection services, string dbPath)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        services.AddSingleton<IEmailNotifier, EmailNotifier>();
        services.AddTransient<IMessageStore, LocalMailsMessageStore>();
        
        services.AddHostedService<SmtpBackgroundService>();
        services.AddHostedService<ApiBackgroundService>();

        return services;
    }
}
