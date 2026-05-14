using System.Data;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace Miestamy30._3.Data;

public class DbConnectionFactory(IConfiguration configuration)
{
    private readonly string _connectionString = BuildConnectionString(configuration);

    private static string BuildConnectionString(IConfiguration configuration)
    {
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (!string.IsNullOrEmpty(databaseUrl))
            return ConvertDatabaseUrl(databaseUrl);

        return configuration.GetConnectionString("DefaultConnection")
               ?? throw new InvalidOperationException("No connection string found.");
    }

    // Railway gives: postgresql://user:password@host:port/dbname
    // Npgsql needs:  Host=...;Port=...;Database=...;Username=...;Password=...
    private static string ConvertDatabaseUrl(string databaseUrl)
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':', 2);
        var database = uri.AbsolutePath.TrimStart('/');
        return $"Host={uri.Host};Port={uri.Port};Database={database};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
    }

    public bool IsPostgres => _connectionString.StartsWith("Host=")
                           || _connectionString.StartsWith("Server=");

    public IDbConnection Create() => IsPostgres
        ? new NpgsqlConnection(_connectionString)
        : new SqliteConnection(_connectionString);
}
