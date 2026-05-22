using System.Globalization;
using System.Net;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Miestamy30._3.Services.Scrapers;

public class StaraTrznicaScraper(IHttpClientFactory httpFactory, ILogger<StaraTrznicaScraper> logger) : IEventScraper
{
    private const string BaseUrl = "https://staratrznica.sk";
    private const string ProgramUrl = "https://staratrznica.sk/sk/program/";
    private const string VenueNazov = "Stará Tržnica";
    private const double Lat = 48.1442;
    private const double Lng = 17.1107;
    private const string Adresa = "Námestie SNP 25, Bratislava";

    private static readonly Dictionary<string, int> SlovakMonths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["január"] = 1, ["jan"] = 1,
        ["február"] = 2, ["feb"] = 2,
        ["marec"] = 3, ["mar"] = 3,
        ["apríl"] = 4, ["apr"] = 4,
        ["máj"] = 5,
        ["jún"] = 6,
        ["júl"] = 7,
        ["august"] = 8, ["aug"] = 8,
        ["september"] = 9, ["sep"] = 9,
        ["október"] = 10, ["okt"] = 10,
        ["november"] = 11, ["nov"] = 11,
        ["december"] = 12, ["dec"] = 12,
    };

    public async Task<IReadOnlyList<ScrapedEvent>> ScrapeAsync(CancellationToken ct = default)
    {
        var client = httpFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; MiestaMy/1.0)");

        string html;
        try { html = await client.GetStringAsync(ProgramUrl, ct); }
        catch (Exception ex)
        {
            logger.LogError(ex, "StaraTrznicaScraper: failed to fetch {Url}", ProgramUrl);
            return [];
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var rawItems = new List<(string sourceUrl, string title, string datumOd, string? datumDo)>();
        var anchors = doc.DocumentNode.SelectNodes("//a[contains(@class,'event-link')]");
        if (anchors is null) return [];

        foreach (var anchor in anchors)
        {
            try
            {
                var href = anchor.GetAttributeValue("href", "");
                if (string.IsNullOrEmpty(href)) continue;
                var sourceUrl = href.StartsWith("http") ? href : BaseUrl + href;

                // Date is the last segment of the URL: /sk/program/slug/2026-05-16
                var segments = href.TrimEnd('/').Split('/');
                var lastSegment = segments.Last();
                if (!DateTime.TryParseExact(lastSegment, "yyyy-MM-dd", null, DateTimeStyles.None, out var parsedDate))
                    continue;
                if (parsedDate < DateTime.Today) continue;
                var datumOd = parsedDate.ToString("yyyy-MM-dd");

                // Title is the last <strong> (without class event-date)
                var strongs = anchor.SelectNodes(".//strong");
                var titleNode = strongs?.LastOrDefault(n => string.IsNullOrEmpty(n.GetAttributeValue("class", "")));
                var title = WebUtility.HtmlDecode(titleNode?.InnerText.Trim() ?? "");
                if (string.IsNullOrEmpty(title)) continue;

                rawItems.Add((sourceUrl, title, datumOd, null));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "StaraTrznicaScraper: error parsing list item");
            }
        }

        // Fetch detail pages for images and descriptions
        var events = new List<ScrapedEvent>();
        foreach (var (sourceUrl, title, datumOd, datumDo) in rawItems)
        {
            string? imageUrl = null;
            string? popis = null;
            try
            {
                await Task.Delay(500, ct);
                var detailHtml = await client.GetStringAsync(sourceUrl, ct);
                var detailDoc = new HtmlDocument();
                detailDoc.LoadHtml(detailHtml);

                imageUrl = detailDoc.DocumentNode
                    .SelectSingleNode("//meta[@property='og:image']")
                    ?.GetAttributeValue("content", null);

                popis = detailDoc.DocumentNode
                    .SelectSingleNode("//meta[@property='og:description']")
                    ?.GetAttributeValue("content", null);
                if (!string.IsNullOrWhiteSpace(popis))
                    popis = WebUtility.HtmlDecode(popis).Trim();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "StaraTrznicaScraper: could not fetch detail for {Url}", sourceUrl);
            }

            events.Add(new ScrapedEvent
            {
                Nazov      = title,
                Popis      = popis,
                DatumOd    = datumOd,
                DatumDo    = datumDo,
                Adresa     = Adresa,
                Lat        = Lat,
                Lng        = Lng,
                ImageUrl   = imageUrl,
                SourceUrl  = sourceUrl,
                VenueNazov = VenueNazov,
            });
        }

        logger.LogInformation("StaraTrznicaScraper: scraped {Count} events", events.Count);
        return events;
    }
}
