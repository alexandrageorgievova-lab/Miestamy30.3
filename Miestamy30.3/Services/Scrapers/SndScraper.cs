using System.Net;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Miestamy30._3.Services.Scrapers;

public class SndScraper(IHttpClientFactory httpFactory, ILogger<SndScraper> logger) : IEventScraper
{
    private const string ProgramUrl = "https://snd.sk/program";
    private const string BaseUrl = "https://snd.sk";
    private const string VenueNazov = "SND";
    private const double Lat = 48.1404;
    private const double Lng = 17.1139;
    private const string Adresa = "Pribinova 17, Bratislava";

    private static readonly Dictionary<string, int> SlovakMonths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["jan"] = 1, ["januára"] = 1, ["január"] = 1,
        ["feb"] = 2, ["februára"] = 2, ["február"] = 2,
        ["mar"] = 3, ["marca"] = 3, ["marec"] = 3,
        ["apr"] = 4, ["apríla"] = 4, ["apríl"] = 4,
        ["máj"] = 5, ["mája"] = 5,
        ["jún"] = 6, ["júna"] = 6,
        ["júl"] = 7, ["júla"] = 7,
        ["aug"] = 8, ["augusta"] = 8, ["august"] = 8,
        ["sep"] = 9, ["septembra"] = 9, ["september"] = 9,
        ["okt"] = 10, ["októbra"] = 10, ["október"] = 10,
        ["nov"] = 11, ["novembra"] = 11, ["november"] = 11,
        ["dec"] = 12, ["decembra"] = 12, ["december"] = 12,
    };

    public async Task<IReadOnlyList<ScrapedEvent>> ScrapeAsync(CancellationToken ct = default)
    {
        var client = httpFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; MiestaMy/1.0)");

        string html;
        try { html = await client.GetStringAsync(ProgramUrl, ct); }
        catch (Exception ex)
        {
            logger.LogError(ex, "SndScraper: failed to fetch {Url}", ProgramUrl);
            return [];
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var events = new List<ScrapedEvent>();
        var items = doc.DocumentNode.SelectNodes("//div[contains(@class,'performance')]");
        if (items is null) return events;

        foreach (var item in items)
        {
            try
            {
                var titleNode = item.SelectSingleNode(".//div[@class='title']//span[@class='value']")
                    ?? item.SelectSingleNode(".//div[contains(@class,'title')]//a");
                var title = WebUtility.HtmlDecode(titleNode?.InnerText.Trim() ?? "");
                if (string.IsNullOrEmpty(title)) continue;

                var dateNode = item.SelectSingleNode(".//*[@class='on-date']")
                    ?? item.SelectSingleNode(".//*[contains(@class,'date')]");
                var dateRaw = WebUtility.HtmlDecode(dateNode?.InnerText.Trim() ?? "");
                var datumOd = ParseDate(dateRaw);
                if (datumOd is null) continue;

                if (DateTime.TryParse(datumOd, out var d) && d < DateTime.Today) continue;

                var imgNode = item.SelectSingleNode(".//div[contains(@class,'image')]//img")
                    ?? item.SelectSingleNode(".//img");
                var imgSrc = imgNode?.GetAttributeValue("src", null);
                if (imgSrc != null && !imgSrc.StartsWith("http"))
                    imgSrc = BaseUrl + imgSrc;

                var linkNode = item.SelectSingleNode(".//a[contains(@href,'/program')]")
                    ?? item.SelectSingleNode(".//a");
                var href = linkNode?.GetAttributeValue("href", null);
                var sourceUrl = href is null ? null
                    : href.StartsWith("http") ? href : BaseUrl + href;
                if (string.IsNullOrEmpty(sourceUrl)) continue;

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
                logger.LogWarning(ex, "SndScraper: error parsing performance item");
            }
        }

        logger.LogInformation("SndScraper: scraped {Count} events", events.Count);
        return events;
    }

    // Handles: "17. máj 2026", "17.5.2026", "17. 5. 2026", "17. 5."
    private static string? ParseDate(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        raw = raw.Replace("\n", " ").Replace("\r", " ").Trim();

        // Try "17. máj 2026" or "17. máj"
        var parts = raw.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            var dayPart = parts[0].TrimEnd('.');
            if (int.TryParse(dayPart, out var day))
            {
                // parts[1] = month name or number
                var monthPart = parts[1].TrimEnd('.');
                int month;
                if (!int.TryParse(monthPart, out month))
                    if (!SlovakMonths.TryGetValue(monthPart, out month)) return null;

                var year = parts.Length >= 3 && int.TryParse(parts[2], out var y) ? y : DateTime.Today.Year;
                try
                {
                    var date = new DateTime(year, month, day);
                    if (date < DateTime.Today.AddDays(-30) && parts.Length < 3) date = date.AddYears(1);
                    return date.ToString("yyyy-MM-dd");
                }
                catch { return null; }
            }
        }

        // Try "17.5.2026" or "17.05.2026"
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
