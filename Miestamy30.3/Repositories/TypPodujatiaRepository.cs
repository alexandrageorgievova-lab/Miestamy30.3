using Dapper;
using Miestamy30._3.Data;
using Miestamy30._3.Models;
using Miestamy30._3.Repositories.Interfaces;

namespace Miestamy30._3.Repositories;

public class TypPodujatiaRepository(DbConnectionFactory factory) : ITypPodujatiaRepository
{
    public async Task<int> Create(TypPodujatia typ)
    {
        using var conn = factory.Create();
        var sql = factory.IsPostgres
            ? "INSERT INTO TypPodujatia (Nazov) VALUES (@Nazov) RETURNING Id"
            : "INSERT INTO TypPodujatia (Nazov) VALUES (@Nazov); SELECT last_insert_rowid();";
        return await conn.ExecuteScalarAsync<int>(sql, typ);
    }

    public async Task<IEnumerable<TypPodujatia>> GetAll()
    {
        using var conn = factory.Create();
        return await conn.QueryAsync<TypPodujatia>("SELECT Id, Nazov FROM TypPodujatia ORDER BY Nazov");
    }

    public async Task<TypPodujatia?> GetById(int id)
    {
        using var conn = factory.Create();
        return await conn.QuerySingleOrDefaultAsync<TypPodujatia>(
            "SELECT Id, Nazov FROM TypPodujatia WHERE Id = @Id", new { Id = id });
    }

    public async Task Update(TypPodujatia typ)
    {
        using var conn = factory.Create();
        await conn.ExecuteAsync("UPDATE TypPodujatia SET Nazov = @Nazov WHERE Id = @Id", typ);
    }

    public async Task Delete(int id)
    {
        using var conn = factory.Create();
        await conn.ExecuteAsync("DELETE FROM TypPodujatia WHERE Id = @Id", new { Id = id });
    }
}
