using CellScope.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CellScope.ApiTests;

public class CellScopeTestFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"cellscope_test_{Guid.NewGuid():N}.db";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<CellScopeDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Register distinct SQLite test database
            services.AddDbContext<CellScopeDbContext>(options =>
            {
                options.UseSqlite($"Data Source={_dbName}");
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try
        {
            if (File.Exists(_dbName)) File.Delete(_dbName);
        }
        catch { }
    }
}
