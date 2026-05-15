using Dapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Miestamy30._3.Data;
using Miestamy30._3.Services.Scrapers;

namespace Miestamy30._3.Services;

public class EventScraperService(
    IEnumerable<IEventScraper> scrapers,
    DbConnectionFactory dbFactory,
    ILogger<EventScraperService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunCycleAsync(stoppingToken); }
            catch (Exception ex) { logger.LogError(ex, "Scraper cycle failed"); }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task RunCycleAsync(CancellationToken ct)
    {
        logger.LogInformation("Starting event scraper cycle");
        using var conn = dbFactory.Create();

        // First run: remove seed/fake events (SourceUrl IS NULL) before inserting real ones
        var scrapedCount = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM Podujatie WHERE SourceUrl IS NOT NULL");
        if (scrapedCount == 0)
        {
            await conn.ExecuteAsync("DELETE FROM Podujatie WHERE SourceUrl IS NULL");
            logger.LogInformation("Removed seed events on first scraper run");
        }

        // Load lookup tables
        var typIds = (await conn.QueryAsync<(int Id, string Nazov)>("SELECT Id, Nazov FROM TypPodujatia"))
            .ToDictionary(t => t.Nazov, t => t.Id);
        var venueIds = (await conn.QueryAsync<(int Id, string Nazov)>("SELECT Id, Nazov FROM Miesto"))
            .ToDictionary(t => t.Nazov, t => t.Id);

        var insertSql = dbFactory.IsPostgres
            ? @"INSERT INTO Podujatie (Nazov, Popis, DatumOd, DatumDo, Adresa, Lat, Lng, MiestoId, ImageUrl, SourceUrl)
               VALUES (@Nazov, @Popis, @DatumOd, @DatumDo, @Adresa, @Lat, @Lng, @MiestoId, @ImageUrl, @SourceUrl)
               RETURNING Id"
            : @"INSERT INTO Podujatie (Nazov, Popis, DatumOd, DatumDo, Adresa, Lat, Lng, MiestoId, ImageUrl, SourceUrl)
               VALUES (@Nazov, @Popis, @DatumOd, @DatumDo, @Adresa, @Lat, @Lng, @MiestoId, @ImageUrl, @SourceUrl);
               SELECT last_insert_rowid();";
        var linkTypSql = dbFactory.IsPostgres
            ? "INSERT INTO PodujatieTyp (PodujatieId, TypId) VALUES (@PodujatieId, @TypId) ON CONFLICT DO NOTHING"
            : "INSERT OR IGNORE INTO PodujatieTyp (PodujatieId, TypId) VALUES (@PodujatieId, @TypId)";

        int newCount = 0;
        foreach (var scraper in scrapers)
        {
            IReadOnlyList<ScrapedEvent> scraped;
            try { scraped = await scraper.ScrapeAsync(ct); }
            catch (Exception ex)
            {
                logger.LogError(ex, "Scraper {Name} failed", scraper.GetType().Name);
                continue;
            }

            foreach (var ev in scraped)
            {
                if (string.IsNullOrEmpty(ev.SourceUrl)) continue;

                var exists = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Podujatie WHERE SourceUrl = @SourceUrl",
                    new { ev.SourceUrl });
                if (exists > 0) continue;

                int? miestoId = ev.VenueNazov != null && venueIds.TryGetValue(ev.VenueNazov, out var vid) ? vid : null;

                var podujatieId = await conn.ExecuteScalarAsync<int>(insertSql, new
                {
                    ev.Nazov, ev.Popis, ev.DatumOd, ev.DatumDo,
                    ev.Adresa, ev.Lat, ev.Lng,
                    MiestoId = miestoId,
                    ev.ImageUrl, ev.SourceUrl,
                });

                var typNazov = GuessTyp(ev.Nazov, ev.Popis);
                if (typIds.TryGetValue(typNazov, out var typId))
                    await conn.ExecuteAsync(linkTypSql, new { PodujatieId = podujatieId, TypId = typId });

                newCount++;
            }
        }

        // Remove past scraped events
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var deleted = await conn.ExecuteAsync(
            "DELETE FROM Podujatie WHERE SourceUrl IS NOT NULL AND DatumOd < @Today",
            new { Today = today });

        logger.LogInformation("Scraper cycle done: {New} new, {Deleted} old removed", newCount, deleted);
    }

    private static string GuessTyp(string nazov, string? popis)
    {
        var t = $"{nazov} {popis}".ToLowerInvariant();
        if (t.Contains("rave") || t.Contains("párty") || t.Contains("party") || t.Contains("techno") || t.Contains("dnb") || t.Contains("drum") || t.Contains("club"))
            return "Párty / Rave";
        if (t.Contains("workshop") || t.Contains("dielňa"))
            return "Workshop";
        if (t.Contains("vernisáž") || t.Contains("vernissage"))
            return "Vernisáž";
        if (t.Contains("výstava"))
            return "Výstava";
        if (t.Contains("trh") || t.Contains("market") || t.Contains("bazár"))
            return "Trh / Market";
        if (t.Contains("film") || t.Contains("kino") || t.Contains("premietanie"))
            return "Filmové premietanie";
        if (t.Contains("divadl"))
            return "Divadelné predstavenie";
        if (t.Contains("diskusi") || t.Contains("panel") || t.Contains("podcast") || t.Contains("debat"))
            return "Diskusia / Panel";
        if (t.Contains("festival"))
            return "Festival";
        if (t.Contains("stand-up") || t.Contains("standup"))
            return "Stand-up";
        return "Koncert";
    }
}
