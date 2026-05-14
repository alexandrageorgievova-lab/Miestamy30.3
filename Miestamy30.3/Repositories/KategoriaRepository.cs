using Dapper;
using Miestamy30._3.Data;
using Miestamy30._3.Models;
using Miestamy30._3.Repositories.Interfaces;

namespace Miestamy30._3.Repositories;

public class KategoriaRepository(DbConnectionFactory factory) : IKategoriaRepository
{
    public async Task<int> Create(Kategoria kategoria)
    {
        using var conn = factory.Create();
        var sql = factory.IsPostgres
            ? "INSERT INTO Kategoria (Nazov) VALUES (@Nazov) RETURNING Id"
            : "INSERT INTO Kategoria (Nazov) VALUES (@Nazov); SELECT last_insert_rowid();";
        return await conn.ExecuteScalarAsync<int>(sql, kategoria);
    }

    public async Task<IEnumerable<Kategoria>> GetAll()
    {
        using var conn = factory.Create();
        return await conn.QueryAsync<Kategoria>("SELECT Id, Nazov FROM Kategoria ORDER BY Id");
    }

    public async Task<Kategoria?> GetById(int id)
    {
        using var conn = factory.Create();
        return await conn.QuerySingleOrDefaultAsync<Kategoria>(
            "SELECT Id, Nazov FROM Kategoria WHERE Id = @Id", new { Id = id });
    }

    public async Task<Kategoria?> GetByNazov(string nazov)
    {
        using var conn = factory.Create();
        return await conn.QuerySingleOrDefaultAsync<Kategoria>(
            "SELECT Id, Nazov FROM Kategoria WHERE Nazov = @Nazov", new { Nazov = nazov });
    }

    public async Task Update(Kategoria kategoria)
    {
        using var conn = factory.Create();
        await conn.ExecuteAsync(
            "UPDATE Kategoria SET Nazov = @Nazov WHERE Id = @Id", kategoria);
    }

    public async Task Delete(int id)
    {
        using var conn = factory.Create();
        await conn.ExecuteAsync("DELETE FROM Kategoria WHERE Id = @Id", new { Id = id });
    }
}
