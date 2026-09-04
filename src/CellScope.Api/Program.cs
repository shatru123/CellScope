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
string? rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? Environment.GetEnvironmentVariable("DATABASE_URL");

string? npgsqlConn = DatabaseConfig.FormatPostgreSqlConnectionString(rawConnectionString);

builder.Services.AddDbContext<CellScopeDbContext>(options =>
{
    if (DatabaseConfig.IsPostgreSql(rawConnectionString) && !string.IsNullOrEmpty(npgsqlConn))
    {
        options.UseNpgsql(npgsqlConn, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
        });
    }
    else
    {
        options.UseSqlite(rawConnectionString ?? "Data Source=cellscope.db");
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
builder.Services.AddScoped<ISecurityAnalysisService, SecurityAnalysisService>();
builder.Services.AddScoped<ICellularRadioAnalysisService, CellularRadioAnalysisService>();
builder.Services.AddScoped<IPrivate5gCoreService, Private5gCoreService>();
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
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<CellScopeDbContext>();
        await db.Database.EnsureCreatedAsync();
        
        bool isPostgres = db.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;
        
        var schemaMigrations = isPostgres ? new[]
        {
            "ALTER TABLE \"NetworkDevices\" ADD COLUMN IF NOT EXISTS \"PhoneNumber\" TEXT;",
            "ALTER TABLE \"TowerLocations\" ADD COLUMN IF NOT EXISTS \"Area\" TEXT;",
            "ALTER TABLE \"TowerLocations\" ADD COLUMN IF NOT EXISTS \"StreetAddress\" TEXT;",
            "ALTER TABLE \"TowerLocations\" ADD COLUMN IF NOT EXISTS \"City\" TEXT;",
            "ALTER TABLE \"TowerLocations\" ADD COLUMN IF NOT EXISTS \"PostalCode\" TEXT;"
        } : new[]
        {
            "ALTER TABLE \"NetworkDevices\" ADD COLUMN \"PhoneNumber\" TEXT;",
            "ALTER TABLE \"TowerLocations\" ADD COLUMN \"Area\" TEXT;",
            "ALTER TABLE \"TowerLocations\" ADD COLUMN \"StreetAddress\" TEXT;",
            "ALTER TABLE \"TowerLocations\" ADD COLUMN \"City\" TEXT;",
            "ALTER TABLE \"TowerLocations\" ADD COLUMN \"PostalCode\" TEXT;"
        };

        foreach (var sql in schemaMigrations)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync(sql);
            }
            catch { }
        }

        var towerService = scope.ServiceProvider.GetRequiredService<ITowerService>();
        await towerService.SeedDefaultTowersAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Database Initialization Notice] {ex.Message}");
    }
}

app.UseSerilogRequestLogging();
app.UseCors("AllowAll");

app.UseRouting();

app.MapControllers();
app.MapHub<NetworkHub>("/hubs/network");

Log.Information("CellScope API & SignalR Engine is starting...");
app.Run();
public partial class Program { }
