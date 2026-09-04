using CellScope.Api.Hubs;
using CellScope.Api.Services;
using CellScope.Application.Interfaces;
using CellScope.Infrastructure.Data;
using CellScope.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Configure Database
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

// Register Core Domain & Infrastructure Services
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

// Add SignalR
builder.Services.AddSignalR();

// Add Controllers & OpenAPI
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure CORS
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

// Auto-migrate & Seed Database on startup
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

app.UseSerilogRequestLogging();
app.UseCors("AllowAll");

app.UseRouting();

app.MapControllers();
app.MapHub<NetworkHub>("/hubs/network");

Log.Information("CellScope API & SignalR Engine is starting...");
app.Run();
public partial class Program { }
