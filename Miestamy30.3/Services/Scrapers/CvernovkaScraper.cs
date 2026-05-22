using System.Net;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Miestamy30._3.Services.Scrapers;

public class CvernovkaScraper(IHttpClientFactory httpFactory, ILogger<CvernovkaScraper> logger) : IEventScraper
{
    private const string BaseUrl = "https://novacvernovka.eu";
    private const string ProgramUrl = "https://novacvernovka.eu/program/";
    private const string VenueNazov = "KC Nová Cvernovka";
    private const double Lat = 48.18305933;
    private const double Lng = 17.13178615;
    private const string Adresa = "Račianska 78, Bratislava";

    public async Task<IReadOnlyList<ScrapedEvent>> ScrapeAsync(CancellationToken ct = default)
    {
        var client = httpFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

        string html;
        try { html = await client.GetStringAsync(ProgramUrl, ct); }
        catch (Exception ex)
        {
            logger.LogError(ex, "CvernovkaScraper: failed to fetch {Url}", ProgramUrl);
            return [];
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var events = new List<ScrapedEvent>();

        // Cvernovka WordPress: each event is an <article data-start="YYYYMMDD" data-href="url">
        var articles = doc.DocumentNode.SelectNodes("//article[@data-start and @data-href]");
        if (articles is null)
        {
            logger.LogWarning("CvernovkaScraper: no article[@data-start] found — site structure may have changed");
            return events;
        }

        foreach (var article in articles)
        {
            try
            {
                var sourceUrl = article.GetAttributeValue("data-href", "");
                if (string.IsNullOrEmpty(sourceUrl)) continue;
                if (!sourceUrl.StartsWith("http")) sourceUrl = BaseUrl + sourceUrl;

                var dataStart = article.GetAttributeValue("data-start", "");
                var datumOd = ParseDataStart(dataStart);
                if (datumOd is null) continue;

                var titleNode = article.SelectSingleNode(".//*[contains(@class,'entry-title')]");
                var title = WebUtility.HtmlDecode(titleNode?.InnerText.Trim() ?? "");
                if (string.IsNullOrEmpty(title)) continue;

                var imgNode = article.SelectSingleNode(".//img");
                var imgSrc = imgNode?.GetAttributeValue("src", null);
                if (imgSrc != null && !imgSrc.StartsWith("http"))
                    imgSrc = BaseUrl + imgSrc;

                events.Add(new ScrapedEvent
                {
                    Nazov      = title,
                    DatumOd    = datumOd,
                    Adresa     = Adresa,
                    Lat        = Lat,
                    Lng        = Lng,
                    ImageUrl   = imgSrc,
                    SourceUrl  = sourceUrl,
                    VenueNazov = VenueNazov,
                });
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "CvernovkaScraper: error parsing article");
            }
        }

        events = events.DistinctBy(e => e.SourceUrl).ToList();
        logger.LogInformation("CvernovkaScraper: scraped {Count} events", events.Count);
        return events;
    }

    // Parses "20260523" → "2026-05-23"
    private static string? ParseDataStart(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Length != 8) return null;
        if (!int.TryParse(raw[..4], out var year)) return null;
        if (!int.TryParse(raw[4..6], out var month)) return null;
        if (!int.TryParse(raw[6..8], out var day)) return null;
        try { return new DateTime(year, month, day).ToString("yyyy-MM-dd"); }
        catch { return null; }
    }
}
