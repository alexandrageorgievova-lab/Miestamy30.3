using System.Net;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Miestamy30._3.Services.Scrapers;

public class SngScraper(IHttpClientFactory httpFactory, ILogger<SngScraper> logger) : IEventScraper
{
    private const string EventsUrl = "https://sng.sk/sk/sng-bratislava/vystavy-programy?event_type=event";
    private const string BaseUrl = "https://sng.sk";
    private const string VenueNazov = "SNG";
    private const double Lat = 48.1408;
    private const double Lng = 17.1075;
    private const string Adresa = "Rázusovo nábrežie 2, Bratislava";

    private static readonly Dictionary<string, int> SlovakMonths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["January"] = 1,  ["February"] = 2, ["March"] = 3,   ["April"] = 4,
        ["May"] = 5,       ["June"] = 6,     ["July"] = 7,    ["August"] = 8,
        ["September"] = 9, ["October"] = 10, ["November"] = 11,["December"] = 12,
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
            logger.LogError(ex, "SngScraper: failed to fetch {Url}", EventsUrl);
            return [];
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var events = new List<ScrapedEvent>();

        // SNG uses Vue SSR: events are in #eventsContainer > a
        var container = doc.GetElementbyId("eventsContainer");
        var anchors = container?.SelectNodes(".//a[@href]")
            ?? doc.DocumentNode.SelectNodes("//a[contains(@href,'/podujatia/') or contains(@href,'/vystavy/')]");
        if (anchors is null) return events;

        foreach (var anchor in anchors)
        {
            try
            {
                var href = anchor.GetAttributeValue("href", "");
                if (string.IsNullOrEmpty(href)) continue;
                // Fix localhost URLs that SNG embeds
                if (href.Contains("localhost")) href = href.Replace("http://localhost", BaseUrl);
                var sourceUrl = href.StartsWith("http") ? href : BaseUrl + href;

                // Title: span with font-sng class
                var titleNode = anchor.SelectSingleNode(".//*[contains(@class,'font-sng')]");
                var title = WebUtility.HtmlDecode(titleNode?.InnerText.Trim() ?? "");
                if (string.IsNullOrEmpty(title)) continue;

                // Date: small text like "17. May 2026 / 15.00"
                var dateNode = anchor.SelectNodes(".//*[contains(@class,'text-sm')]")
                    ?.LastOrDefault();
                var dateRaw = WebUtility.HtmlDecode(dateNode?.InnerText.Trim() ?? "");
                var datumOd = ParseDate(dateRaw);
                if (datumOd is null) continue;

                if (DateTime.TryParse(datumOd, out var d) && d < DateTime.Today) continue;

                // Image
                var imgNode = anchor.SelectSingleNode(".//img");
                var imgSrc = imgNode?.GetAttributeValue("src", null);
                if (imgSrc != null && imgSrc.Contains("localhost"))
                    imgSrc = imgSrc.Replace("http://localhost", BaseUrl);

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
                logger.LogWarning(ex, "SngScraper: error parsing event item");
            }
        }

        logger.LogInformation("SngScraper: scraped {Count} events", events.Count);
        return events;
    }

    // Handles "17. May 2026 / 15.00", "17. máj 2026", "17.5.2026"
    private static string? ParseDate(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        // Strip time part after "/"
        var slashIdx = raw.IndexOf('/');
        if (slashIdx > 0) raw = raw[..slashIdx];
        raw = raw.Trim();

        var parts = raw.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            var dayPart = parts[0].TrimEnd('.');
            if (int.TryParse(dayPart, out var day))
            {
                var monthPart = parts[1].TrimEnd('.');
                int month;
                if (!int.TryParse(monthPart, out month))
                    if (!SlovakMonths.TryGetValue(monthPart, out month)) return null;

                var year = parts.Length >= 3 && int.TryParse(parts[2].Trim('.'), out var y) ? y : DateTime.Today.Year;
                try { return new DateTime(year, month, day).ToString("yyyy-MM-dd"); }
                catch { return null; }
            }
        }

        var dotParts = raw.Split('.');
        if (dotParts.Length >= 2 &&
            int.TryParse(dotParts[0].Trim(), out var d2) &&
            int.TryParse(dotParts[1].Trim(), out var m2))
        {
            var y2 = dotParts.Length >= 3 && int.TryParse(dotParts[2].Trim(), out var yy) ? yy : DateTime.Today.Year;
            try { return new DateTime(y2, m2, d2).ToString("yyyy-MM-dd"); }
            catch { return null; }
        }

        return null;
    }
}
