using LocalMails.App.Services;
using LocalMails.Server.Data;
using LocalMails.Server.Extensions;
using LocalMails.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;

namespace LocalMails.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Add device-specific services used by the LocalMails.Shared project
        builder.Services.AddSingleton<IFormFactor, FormFactor>();

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddFluentUIComponents();

        // LocalMails Backend Services
        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "localmails.db");
        builder.Services.AddLocalMailsServer(dbPath);

        #if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif
        
        var app = builder.Build();

        // Ensure database is created and BackgroundServices are started
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();

            // IMPORTANT: MAUI does not automatically start BackgroundServices (IHostedService).
            // We must start them manually here.
            var hostedServices = scope.ServiceProvider.GetServices<IHostedService>();
            foreach (var service in hostedServices)
            {
                Task.Run(async () => await service.StartAsync(CancellationToken.None));
            }
        }

        return app;
    }
}
