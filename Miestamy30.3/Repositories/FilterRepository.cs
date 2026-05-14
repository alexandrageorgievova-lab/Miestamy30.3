using Dapper;
using Miestamy30._3.Data;
using Miestamy30._3.Models;
using Miestamy30._3.Repositories.Interfaces;

namespace Miestamy30._3.Repositories;

public class FilterRepository(DbConnectionFactory factory) : IFilterRepository
{
    public async Task<int> Create(Filter filter)
    {
        using var conn = factory.Create();
        var sql = factory.IsPostgres
            ? "INSERT INTO Filter (Nazov, KategoriaId) VALUES (@Nazov, @KategoriaId) RETURNING Id"
            : "INSERT INTO Filter (Nazov, KategoriaId) VALUES (@Nazov, @KategoriaId); SELECT last_insert_rowid();";
        return await conn.ExecuteScalarAsync<int>(sql, filter);
    }

    public async Task<IEnumerable<Filter>> GetAll()
    {
        using var conn = factory.Create();
        return await conn.QueryAsync<Filter>(
            "SELECT Id, Nazov, KategoriaId FROM Filter ORDER BY KategoriaId, Nazov");
    }

    public async Task<Filter?> GetById(int id)
    {
        using var conn = factory.Create();
        return await conn.QuerySingleOrDefaultAsync<Filter>(
            "SELECT Id, Nazov, KategoriaId FROM Filter WHERE Id = @Id", new { Id = id });
    }

    // JOIN 1 – filter + kategoria
    public async Task<IEnumerable<Filter>> GetByKategoriaId(int kategoriaId)
    {
        using var conn = factory.Create();
        return await conn.QueryAsync<Filter>(@"
            SELECT f.Id, f.Nazov, f.KategoriaId
            FROM Filter f
            INNER JOIN Kategoria k ON f.KategoriaId = k.Id
            WHERE k.Id = @KategoriaId
            ORDER BY f.Nazov",
            new { KategoriaId = kategoriaId });
    }

    public async Task Update(Filter filter)
    {
        using var conn = factory.Create();
        await conn.ExecuteAsync(
            "UPDATE Filter SET Nazov = @Nazov, KategoriaId = @KategoriaId WHERE Id = @Id", filter);
    }

    public async Task Delete(int id)
    {
        using var conn = factory.Create();
        await conn.ExecuteAsync("DELETE FROM Filter WHERE Id = @Id", new { Id = id });
    }
}
