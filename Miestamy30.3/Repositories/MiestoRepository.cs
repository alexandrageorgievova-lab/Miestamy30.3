using Dapper;
using Miestamy30._3.Data;
using Miestamy30._3.Models;
using Miestamy30._3.Models.Dtos;
using Miestamy30._3.Repositories.Interfaces;

namespace Miestamy30._3.Repositories;

public class MiestoRepository(DbConnectionFactory factory) : IMiestoRepository
{
    // ── Basic CRUD ───────────────────────────────────────────────────────────────

    public async Task<int> Create(Miesto miesto)
    {
        using var conn = factory.Create();
        var sql = factory.IsPostgres
            ? @"INSERT INTO Miesto (Nazov, Adresa, Lat, Lng, Popis, WebUrl)
                VALUES (@Nazov, @Adresa, @Lat, @Lng, @Popis, @WebUrl) RETURNING Id"
            : @"INSERT INTO Miesto (Nazov, Adresa, Lat, Lng, Popis, WebUrl)
                VALUES (@Nazov, @Adresa, @Lat, @Lng, @Popis, @WebUrl);
                SELECT last_insert_rowid();";
        return await conn.ExecuteScalarAsync<int>(sql, miesto);
    }

    public async Task<IEnumerable<Miesto>> GetAll()
    {
        using var conn = factory.Create();
        return await conn.QueryAsync<Miesto>(
            "SELECT Id, Nazov, Adresa, Lat, Lng, Popis, WebUrl FROM Miesto ORDER BY Nazov");
    }

    public async Task<Miesto?> GetById(int id)
    {
        using var conn = factory.Create();
        return await conn.QuerySingleOrDefaultAsync<Miesto>(
            "SELECT Id, Nazov, Adresa, Lat, Lng, Popis, WebUrl FROM Miesto WHERE Id = @Id",
            new { Id = id });
    }

    public async Task Update(Miesto miesto)
    {
        using var conn = factory.Create();
        await conn.ExecuteAsync(@"
            UPDATE Miesto
            SET Nazov = @Nazov, Adresa = @Adresa, Lat = @Lat, Lng = @Lng,
                Popis = @Popis, WebUrl = @WebUrl
            WHERE Id = @Id",
            miesto);
    }

    public async Task Delete(int id)
    {
        using var conn = factory.Create();
        await conn.ExecuteAsync("DELETE FROM Miesto WHERE Id = @Id", new { Id = id });
    }

    // ── JOIN query 1 – miesta podľa kategórie ───────────────────────────────────
    // Spája: Miesto ↔ MiestoKategoria ↔ Kategoria
    public async Task<IEnumerable<MiestoSummaryDto>> GetByKategoria(string kategoriaNazov)
    {
        using var conn = factory.Create();
        return await conn.QueryAsync<MiestoSummaryDto>(@"
            SELECT m.Id, m.Nazov, k.Nazov AS HlavnaKategoria, m.Lat, m.Lng
            FROM Miesto m
            INNER JOIN MiestoKategoria mk ON m.Id = mk.MiestoId
            INNER JOIN Kategoria k       ON mk.KategoriaId = k.Id
            WHERE k.Nazov = @KategoriaNazov
            ORDER BY m.Nazov",
            new { KategoriaNazov = kategoriaNazov });
    }

    // ── JOIN query 2 – miesta podľa filtra ──────────────────────────────────────
    // Spája: Miesto ↔ MiestoFilter ↔ Filter ↔ Kategoria
    public async Task<IEnumerable<MiestoSummaryDto>> GetByFilter(string filterNazov)
    {
        using var conn = factory.Create();
        return await conn.QueryAsync<MiestoSummaryDto>(@"
            SELECT DISTINCT m.Id, m.Nazov, k2.Nazov AS HlavnaKategoria, m.Lat, m.Lng
            FROM Miesto m
            INNER JOIN MiestoFilter    mf ON m.Id       = mf.MiestoId
            INNER JOIN Filter           f ON mf.FilterId = f.Id
            INNER JOIN Kategoria        k ON f.KategoriaId = k.Id
            INNER JOIN MiestoKategoria mk ON m.Id = mk.MiestoId AND mk.JeHlavna = 1
            INNER JOIN Kategoria       k2 ON mk.KategoriaId = k2.Id
            WHERE f.Nazov = @FilterNazov
            ORDER BY m.Nazov",
            new { FilterNazov = filterNazov });
    }

    // ── JOIN query 3 – detail miesta so všetkými kategóriami a filtrami ─────────
    // Spája: Miesto ↔ MiestoKategoria ↔ Kategoria + MiestoFilter ↔ Filter
    public async Task<MiestoDetailDto?> GetDetailById(int id)
    {
        using var conn = factory.Create();

        var miesto = await GetById(id);
        if (miesto is null) return null;

        var kategorie = await conn.QueryAsync<KategoriaNazovDto>(@"
            SELECT k.Nazov
            FROM MiestoKategoria mk
            INNER JOIN Kategoria k ON mk.KategoriaId = k.Id
            WHERE mk.MiestoId = @Id
            ORDER BY mk.JeHlavna DESC, k.Nazov",
            new { Id = id });

        var filtreRows = await conn.QueryAsync<FilterRowDto>(@"
            SELECT k.Nazov AS KategoriaNazov, f.Nazov AS FilterNazov
            FROM MiestoFilter mf
            INNER JOIN Filter    f ON mf.FilterId    = f.Id
            INNER JOIN Kategoria k ON f.KategoriaId  = k.Id
            WHERE mf.MiestoId = @Id
            ORDER BY k.Nazov, f.Nazov",
            new { Id = id });

        var filtre = filtreRows
            .GroupBy(r => r.KategoriaNazov)
            .ToDictionary(g => g.Key, g => g.Select(r => r.FilterNazov).ToList());

        return new MiestoDetailDto
        {
            Id      = miesto.Id,
            Nazov   = miesto.Nazov,
            Adresa  = miesto.Adresa,
            Lat     = miesto.Lat,
            Lng     = miesto.Lng,
            Popis   = miesto.Popis,
            WebUrl  = miesto.WebUrl,
            Kategorie = kategorie.Select(k => k.Nazov).ToList(),
            Filtre  = filtre,
        };
    }

    // ── Junction table helpers ───────────────────────────────────────────────────

    public async Task AddKategoria(int miestoId, int kategoriaId, bool jeHlavna = false)
    {
        using var conn = factory.Create();
        var sql = factory.IsPostgres
            ? @"INSERT INTO MiestoKategoria (MiestoId, KategoriaId, JeHlavna)
                VALUES (@MiestoId, @KategoriaId, @JeHlavna) ON CONFLICT DO NOTHING"
            : @"INSERT OR IGNORE INTO MiestoKategoria (MiestoId, KategoriaId, JeHlavna)
                VALUES (@MiestoId, @KategoriaId, @JeHlavna)";
        await conn.ExecuteAsync(sql,
            new { MiestoId = miestoId, KategoriaId = kategoriaId, JeHlavna = jeHlavna ? 1 : 0 });
    }

    public async Task AddFilter(int miestoId, int filterId)
    {
        using var conn = factory.Create();
        var sql = factory.IsPostgres
            ? @"INSERT INTO MiestoFilter (MiestoId, FilterId)
                VALUES (@MiestoId, @FilterId) ON CONFLICT DO NOTHING"
            : @"INSERT OR IGNORE INTO MiestoFilter (MiestoId, FilterId)
                VALUES (@MiestoId, @FilterId)";
        await conn.ExecuteAsync(sql,
            new { MiestoId = miestoId, FilterId = filterId });
    }

    public async Task RemoveAllKategorie(int miestoId)
    {
        using var conn = factory.Create();
        await conn.ExecuteAsync(
            "DELETE FROM MiestoKategoria WHERE MiestoId = @MiestoId", new { MiestoId = miestoId });
    }

    public async Task RemoveAllFiltre(int miestoId)
    {
        using var conn = factory.Create();
        await conn.ExecuteAsync(
            "DELETE FROM MiestoFilter WHERE MiestoId = @MiestoId", new { MiestoId = miestoId });
    }
}
