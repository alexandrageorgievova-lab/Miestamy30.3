using Dapper;
using Miestamy30._3.Data;
using Miestamy30._3.Models;
using Miestamy30._3.Repositories.Interfaces;

namespace Miestamy30._3.Repositories;

public class EventFilterRepository(DbConnectionFactory factory) : IEventFilterRepository
{
    public async Task<int> Create(EventFilter filter)
    {
        using var conn = factory.Create();
        return await conn.ExecuteScalarAsync<int>(
            "INSERT INTO EventFilter (Nazov) VALUES (@Nazov); SELECT last_insert_rowid();", filter);
    }

    public async Task<IEnumerable<EventFilter>> GetAll()
    {
        using var conn = factory.Create();
        return await conn.QueryAsync<EventFilter>("SELECT Id, Nazov FROM EventFilter ORDER BY Nazov");
    }

    public async Task<EventFilter?> GetById(int id)
    {
        using var conn = factory.Create();
        return await conn.QuerySingleOrDefaultAsync<EventFilter>(
            "SELECT Id, Nazov FROM EventFilter WHERE Id = @Id", new { Id = id });
    }

    public async Task Update(EventFilter filter)
    {
        using var conn = factory.Create();
        await conn.ExecuteAsync("UPDATE EventFilter SET Nazov = @Nazov WHERE Id = @Id", filter);
    }

    public async Task Delete(int id)
    {
        using var conn = factory.Create();
        await conn.ExecuteAsync("DELETE FROM EventFilter WHERE Id = @Id", new { Id = id });
    }
}
