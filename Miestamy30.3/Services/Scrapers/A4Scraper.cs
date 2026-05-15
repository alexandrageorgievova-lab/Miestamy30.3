using System.Net;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Miestamy30._3.Services.Scrapers;

public class A4Scraper(IHttpClientFactory httpFactory, ILogger<A4Scraper> logger) : IEventScraper
{
    private const string EventsUrl = "https://a4.sk/events/";
    private const string VenueNazov = "A4";
    private const double Lat = 48.1463;
    private const double Lng = 17.1035;
    private const string Adresa = "Karpatská 2, Bratislava";

    private static readonly Dictionary<string, int> SlovakMonths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["jan"] = 1, ["feb"] = 2, ["mar"] = 3, ["apr"] = 4,
        ["máj"] = 5, ["jún"] = 6, ["júl"] = 7, ["aug"] = 8,
        ["sep"] = 9, ["okt"] = 10, ["nov"] = 11, ["dec"] = 12,
    };

    public async Task<IReadOnlyList<ScrapedEvent>> ScrapeAsync(CancellationToken ct = default)
    {
        var client = httpFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; MiestaMy/1.0)");

        string html;
        try { html = await client.GetStringAsync(EventsUrl, ct); }
        catch (Exception ex)
        {
            logger.LogError(ex, "A4Scraper: failed to fetch {Url}", EventsUrl);
            return [];
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var events = new List<ScrapedEvent>();
        var items = doc.DocumentNode.SelectNodes("//li[contains(@class,'event-item')]");
        if (items is null) return events;

        foreach (var item in items)
        {
            try
            {
                var linkNode = item.SelectSingleNode(".//a[contains(@class,'faux-link__anchor')]");
                var sourceUrl = linkNode?.GetAttributeValue("href", null);
                if (string.IsNullOrEmpty(sourceUrl)) continue;

                var titleNode = item.SelectSingleNode(".//*[contains(@class,'faux-link__control')]");
                var title = WebUtility.HtmlDecode(titleNode?.InnerText.Trim() ?? "");
                if (string.IsNullOrEmpty(title)) continue;

                var dayText = item.SelectSingleNode(".//*[contains(@class,'event-item__date')]//*[contains(@class,'h3')]")?.InnerText.Trim();
                var monthText = item.SelectSingleNode(".//*[contains(@class,'event-item__date')]//*[contains(@class,'bar__item')]")?.InnerText.Trim();

                var datumOd = ParseDate(dayText, monthText);
                if (datumOd is null) continue;

                var imgSrc = item.SelectSingleNode(".//*[contains(@class,'event-item__thumbnail')]//img")?.GetAttributeValue("src", null);

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
                logger.LogWarning(ex, "A4Scraper: error parsing event item");
            }
        }

        logger.LogInformation("A4Scraper: scraped {Count} events", events.Count);
        return events;
    }

    private static string? ParseDate(string? dayText, string? monthText)
    {
        if (string.IsNullOrWhiteSpace(dayText) || string.IsNullOrWhiteSpace(monthText)) return null;
        if (!int.TryParse(dayText.Trim(), out var day)) return null;

        var monthKey = monthText.Trim().ToLowerInvariant();
        if (!SlovakMonths.TryGetValue(monthKey, out var month)) return null;

        var year = DateTime.Today.Year;
        try
        {
            var date = new DateTime(year, month, day);
            if (date < DateTime.Today.AddDays(-30)) date = date.AddYears(1);
            return date.ToString("yyyy-MM-dd");
        }
        catch { return null; }
    }
}
