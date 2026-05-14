using System.Data;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace Miestamy30._3.Data;

public class DbConnectionFactory(IConfiguration configuration)
{
    private readonly string _connectionString = BuildConnectionString(configuration);

    private static string BuildConnectionString(IConfiguration configuration)
    {
        // Railway (and most PaaS) set DATABASE_URL as postgresql://user:pass@host:port/db
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (!string.IsNullOrEmpty(databaseUrl))
            return databaseUrl;

        return configuration.GetConnectionString("DefaultConnection")
               ?? throw new InvalidOperationException("No connection string found. Set DATABASE_URL or ConnectionStrings:DefaultConnection.");
    }

    private bool IsPostgres => _connectionString.StartsWith("postgresql://")
                            || _connectionString.StartsWith("postgres://")
                            || _connectionString.StartsWith("Host=")
                            || _connectionString.StartsWith("Server=");

    public IDbConnection Create() => IsPostgres
        ? new NpgsqlConnection(_connectionString)
        : new SqliteConnection(_connectionString);
}
