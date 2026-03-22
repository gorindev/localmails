using BlazorBlueprint.Components;
using LocalMails.Shared.Services;
using LocalMails.Web.Components;
using LocalMails.Web.Services;
using LocalMails.Server.Extensions;
using LocalMails.Server.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddBlazorBlueprintComponents();

// Add device-specific services used by the LocalMails.App.Shared project
builder.Services.AddSingleton<IFormFactor, FormFactor>();

// LocalMails Backend Services
string dbPath = Path.Combine(builder.Environment.ContentRootPath, "localmails.db");
builder.Services.AddLocalMailsServer(dbPath);
builder.Services.AddScoped<MailState>();


var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(LocalMails.Shared._Imports).Assembly);

app.Run();
