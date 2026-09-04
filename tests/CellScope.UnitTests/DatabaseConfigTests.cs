using CellScope.Infrastructure.Data;
using Xunit;

namespace CellScope.UnitTests;

public class DatabaseConfigTests
{
    [Fact]
    public void FormatPostgreSqlConnectionString_WithStandardPostgresUrl_FormatsCorrectly()
    {
        string url = "postgres://testuser:secretpass@dpg-c12345-a.oregon-postgres.render.com/cellscope_db";
        string? result = DatabaseConfig.FormatPostgreSqlConnectionString(url);

        Assert.NotNull(result);
        Assert.Contains("Host=dpg-c12345-a.oregon-postgres.render.com", result);
        Assert.Contains("Port=5432", result);
        Assert.Contains("Database=cellscope_db", result);
        Assert.Contains("Username=testuser", result);
        Assert.Contains("Password=secretpass", result);
        Assert.Contains("SSL Mode=Require", result);
        Assert.Contains("Trust Server Certificate=true", result);
    }

    [Fact]
    public void FormatPostgreSqlConnectionString_WithExplicitPort_PreservesPort()
    {
        string url = "postgresql://myuser:mypass@db.host.internal:5433/mydb?sslmode=require";
        string? result = DatabaseConfig.FormatPostgreSqlConnectionString(url);

        Assert.NotNull(result);
        Assert.Contains("Host=db.host.internal", result);
        Assert.Contains("Port=5433", result);
        Assert.Contains("Database=mydb", result);
        Assert.Contains("Username=myuser", result);
        Assert.Contains("Password=mypass", result);
    }

    [Fact]
    public void FormatPostgreSqlConnectionString_WithAdoNetString_AppendsTrustServerCertificate()
    {
        string raw = "Host=db.render.internal;Port=5432;Database=test;Username=u;Password=p";
        string? result = DatabaseConfig.FormatPostgreSqlConnectionString(raw);

        Assert.NotNull(result);
        Assert.Contains("Trust Server Certificate=true", result);
    }

    [Fact]
    public void IsPostgreSql_DetectsPostgreSqlCorrectly()
    {
        Assert.True(DatabaseConfig.IsPostgreSql("postgres://u:p@h/db"));
        Assert.True(DatabaseConfig.IsPostgreSql("postgresql://u:p@h:5432/db"));
        Assert.True(DatabaseConfig.IsPostgreSql("Host=localhost;Database=db;"));
        Assert.False(DatabaseConfig.IsPostgreSql("Data Source=cellscope.db"));
        Assert.False(DatabaseConfig.IsPostgreSql(null));
    }
}
