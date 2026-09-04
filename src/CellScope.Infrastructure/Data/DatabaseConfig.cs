using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace CellScope.Infrastructure.Data;

public static class DatabaseConfig
{
    public static string? FormatPostgreSqlConnectionString(string? rawConnectionString)
    {
        if (string.IsNullOrWhiteSpace(rawConnectionString))
            return null;

        string trimmed = rawConnectionString.Trim();

        // If it starts with postgres:// or postgresql://
        if (trimmed.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) || 
            trimmed.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var uri = new Uri(trimmed);
                var userInfo = uri.UserInfo.Split(':', 2);
                var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
                var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
                var host = uri.Host;
                var port = uri.Port > 0 ? uri.Port : 5432;
                var dbName = uri.AbsolutePath.TrimStart('/');

                // Strip any query params from dbName if present
                if (dbName.Contains('?'))
                {
                    dbName = dbName.Split('?')[0];
                }

                return $"Host={host};Port={port};Database={dbName};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;Pooling=true;Timeout=15;Command Timeout=30;Keepalive=30;";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DatabaseConfig] Failed to parse postgres URI: {ex.Message}");
                return trimmed;
            }
        }

        // If it's standard Host=...; or Server=...; ADO.NET format
        if (trimmed.Contains("Host=", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("Server=", StringComparison.OrdinalIgnoreCase))
        {
            if (!trimmed.Contains("Trust Server Certificate", StringComparison.OrdinalIgnoreCase) && 
                !trimmed.Contains("TrustServerCertificate", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed.TrimEnd(';') + ";Trust Server Certificate=true;";
            }
            if (!trimmed.Contains("SSL Mode", StringComparison.OrdinalIgnoreCase) && 
                !trimmed.Contains("SslMode", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed.TrimEnd(';') + ";SSL Mode=Prefer;";
            }
            return trimmed;
        }

        return trimmed;
    }

    public static bool IsPostgreSql(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return false;
        var s = connectionString.Trim();
        return s.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
               s.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
               s.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
               s.Contains("Server=", StringComparison.OrdinalIgnoreCase);
    }
}
