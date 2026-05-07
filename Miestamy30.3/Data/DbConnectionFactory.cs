using System.Data;
using Microsoft.Data.Sqlite;

namespace Miestamy30._3.Data;

public class DbConnectionFactory(IConfiguration configuration)
{
    private readonly string _connectionString =
        configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    public IDbConnection Create() => new SqliteConnection(_connectionString);
}
