using Dapper;
using Miestamy30._3.Data;
using Miestamy30._3.Models;
using Miestamy30._3.Models.Dtos;
using Miestamy30._3.Repositories.Interfaces;

namespace Miestamy30._3.Repositories;

public class PodujatieRepository(DbConnectionFactory factory) : IPodujatieRepository
{
    public async Task<int> Create(Podujatie p)
    {
        using var conn = factory.Create();
        return await conn.ExecuteScalarAsync<int>(@"
            INSERT INTO Podujatie (Nazov, Popis, DatumOd, DatumDo, Adresa, Lat, Lng, MiestoId)
            VALUES (@Nazov, @Popis, @DatumOd, @DatumDo, @Adresa, @Lat, @Lng, @MiestoId);
            SELECT last_insert_rowid();", p);
    }

    public async Task<IEnumerable<Podujatie>> GetAll()
    {
        using var conn = factory.Create();
        return await conn.QueryAsync<Podujatie>(
            "SELECT * FROM Podujatie ORDER BY DatumOd");
    }

    public async Task<Podujatie?> GetById(int id)
    {
        using var conn = factory.Create();
        return await conn.QuerySingleOrDefaultAsync<Podujatie>(
            "SELECT * FROM Podujatie WHERE Id = @Id", new { Id = id });
    }

    public async Task Update(Podujatie p)
    {
        using var conn = factory.Create();
        await conn.ExecuteAsync(@"
            UPDATE Podujatie SET Nazov=@Nazov, Popis=@Popis, DatumOd=@DatumOd, DatumDo=@DatumDo,
                Adresa=@Adresa, Lat=@Lat, Lng=@Lng, MiestoId=@MiestoId
            WHERE Id=@Id", p);
    }

    public async Task Delete(int id)
    {
        using var conn = factory.Create();
        await conn.ExecuteAsync("DELETE FROM Podujatie WHERE Id = @Id", new { Id = id });
    }

    // ── JOIN 1: podujatia podľa typu ────────────────────────────────────────────
    // Spája: Podujatie ↔ PodujatieTyp ↔ TypPodujatia
    public async Task<IEnumerable<PodujatieSummaryDto>> GetByTyp(string typNazov)
    {
        using var conn = factory.Create();
        return await conn.QueryAsync<PodujatieSummaryDto>(@"
            SELECT p.Id, p.Nazov, p.DatumOd, p.DatumDo, p.Adresa, p.Lat, p.Lng,
                   t.Nazov AS HlavnyTyp
            FROM Podujatie p
            INNER JOIN PodujatieTyp pt ON p.Id  = pt.PodujatieId
            INNER JOIN TypPodujatia  t ON pt.TypId = t.Id
            WHERE t.Nazov = @TypNazov
            ORDER BY p.DatumOd",
            new { TypNazov = typNazov });
    }

    // ── JOIN 2: podujatia podľa filtra ──────────────────────────────────────────
    // Spája: Podujatie ↔ PodujatieFilter ↔ EventFilter + PodujatieTyp ↔ TypPodujatia
    public async Task<IEnumerable<PodujatieSummaryDto>> GetByFilter(string filterNazov)
    {
        using var conn = factory.Create();
        return await conn.QueryAsync<PodujatieSummaryDto>(@"
            SELECT DISTINCT p.Id, p.Nazov, p.DatumOd, p.DatumDo, p.Adresa, p.Lat, p.Lng,
                   t.Nazov AS HlavnyTyp
            FROM Podujatie p
            INNER JOIN PodujatieFilter pf ON p.Id       = pf.PodujatieId
            INNER JOIN EventFilter      f ON pf.FilterId = f.Id
            LEFT JOIN  PodujatieTyp    pt ON p.Id        = pt.PodujatieId
            LEFT JOIN  TypPodujatia     t ON pt.TypId     = t.Id
            WHERE f.Nazov = @FilterNazov
            ORDER BY p.DatumOd",
            new { FilterNazov = filterNazov });
    }

    // ── JOIN 3: detail podujatia so všetkými typmi, filtrami a miestom ──────────
    // Spája: Podujatie ↔ PodujatieTyp ↔ TypPodujatia
    //                  ↔ PodujatieFilter ↔ EventFilter
    //                  ↔ Miesto (LEFT JOIN)
    public async Task<PodujatieDetailDto?> GetDetailById(int id)
    {
        using var conn = factory.Create();

        var p = await GetById(id);
        if (p is null) return null;

        var typy = await conn.QueryAsync<TypNazovDto>(@"
            SELECT t.Nazov
            FROM PodujatieTyp pt
            INNER JOIN TypPodujatia t ON pt.TypId = t.Id
            WHERE pt.PodujatieId = @Id
            ORDER BY t.Nazov",
            new { Id = id });

        var filtre = await conn.QueryAsync<EventFilterNazovDto>(@"
            SELECT f.Nazov
            FROM PodujatieFilter pf
            INNER JOIN EventFilter f ON pf.FilterId = f.Id
            WHERE pf.PodujatieId = @Id
            ORDER BY f.Nazov",
            new { Id = id });

        string? miestoNazov = null;
        if (p.MiestoId.HasValue)
        {
            miestoNazov = await conn.ExecuteScalarAsync<string>(
                "SELECT Nazov FROM Miesto WHERE Id = @Id", new { Id = p.MiestoId });
        }

        return new PodujatieDetailDto
        {
            Id         = p.Id,
            Nazov      = p.Nazov,
            Popis      = p.Popis,
            DatumOd    = p.DatumOd,
            DatumDo    = p.DatumDo,
            Adresa     = p.Adresa,
            Lat        = p.Lat,
            Lng        = p.Lng,
            MiestoNazov = miestoNazov,
            Typy       = typy.Select(t => t.Nazov).ToList(),
            Filtre     = filtre.Select(f => f.Nazov).ToList(),
        };
    }

    public async Task AddTyp(int podujatieId, int typId)
    {
        using var conn = factory.Create();
        await conn.ExecuteAsync(@"
            INSERT OR IGNORE INTO PodujatieTyp (PodujatieId, TypId) VALUES (@PodujatieId, @TypId)",
            new { PodujatieId = podujatieId, TypId = typId });
    }

    public async Task AddFilter(int podujatieId, int filterId)
    {
        using var conn = factory.Create();
        await conn.ExecuteAsync(@"
            INSERT OR IGNORE INTO PodujatieFilter (PodujatieId, FilterId) VALUES (@PodujatieId, @FilterId)",
            new { PodujatieId = podujatieId, FilterId = filterId });
    }
}
