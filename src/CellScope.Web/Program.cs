using CellScope.Api.Hubs;
using CellScope.Api.Services;
using CellScope.Application.DTOs;
using CellScope.Application.Interfaces;
using CellScope.Infrastructure.Data;
using CellScope.Infrastructure.Services;
using CellScope.Web.Components;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddControllers()
    .AddApplicationPart(typeof(CellScope.Api.Controllers.HealthController).Assembly);
builder.Services.AddSignalR();

// Database
string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? Environment.GetEnvironmentVariable("DATABASE_URL");

builder.Services.AddDbContext<CellScopeDbContext>(options =>
{
    if (!string.IsNullOrEmpty(connectionString) && (connectionString.StartsWith("Host=") || connectionString.StartsWith("postgres://") || connectionString.StartsWith("postgresql://")))
    {
        string npgsqlConn = connectionString;
        if (connectionString.StartsWith("postgres://") || connectionString.StartsWith("postgresql://"))
        {
            var uri = new Uri(connectionString);
            var userInfo = uri.UserInfo.Split(':');
            npgsqlConn = $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={(userInfo.Length > 1 ? userInfo[1] : "")};SSL Mode=Prefer;";
        }
        options.UseNpgsql(npgsqlConn);
    }
    else
    {
        options.UseSqlite(connectionString ?? "Data Source=cellscope.db");
    }
});

// Core Domain & Application Services
builder.Services.AddScoped<ICellularService, CellularService>();
builder.Services.AddScoped<ITowerService, TowerService>();
builder.Services.AddScoped<ILocalNetworkService, LocalNetworkService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IDeviceService, DeviceService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDiagnosticsService, DiagnosticsService>();
builder.Services.AddSingleton<IDemoDataService, DemoDataService>();
builder.Services.AddScoped<INotificationPublisher, SignalRNotificationPublisher>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Ensure DB Created & Seeded
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CellScopeDbContext>();
    try
    {
        await db.Database.EnsureCreatedAsync();
        try
        {
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"NetworkDevices\" ADD COLUMN \"PhoneNumber\" TEXT;");
        }
        catch { }
        var towerService = scope.ServiceProvider.GetRequiredService<ITowerService>();
        await towerService.SeedDefaultTowersAsync();
    }
    catch { }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseCors("AllowAll");
app.UseStaticFiles();
app.UseAntiforgery();

app.MapControllers();
app.MapHub<NetworkHub>("/hubs/network");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
