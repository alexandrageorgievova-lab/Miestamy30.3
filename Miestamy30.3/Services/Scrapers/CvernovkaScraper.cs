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
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; MiestaMy/1.0)");

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

        // Try multiple selectors — site may use article cards or plain anchors
        var anchors =
            doc.DocumentNode.SelectNodes("//a[contains(@href,'/program/') and .//*[self::h1 or self::h2 or self::h3 or self::h4]]")
            ?? doc.DocumentNode.SelectNodes("//article[.//a[contains(@href,'/program/')]]//a[contains(@href,'/program/')]")
            ?? doc.DocumentNode.SelectNodes("//a[contains(@href,'/program/')]");

        if (anchors is null) return events;

        foreach (var anchor in anchors)
        {
            try
            {
                var href = anchor.GetAttributeValue("href", "");
                if (string.IsNullOrEmpty(href)) continue;
                var sourceUrl = href.StartsWith("http") ? href : BaseUrl + href;
                // Skip the listing page itself
                if (sourceUrl.TrimEnd('/') == ProgramUrl.TrimEnd('/')) continue;

                // Title: first heading inside anchor
                var titleNode = anchor.SelectSingleNode(".//*[self::h1 or self::h2 or self::h3 or self::h4]");
                var title = WebUtility.HtmlDecode(titleNode?.InnerText.Trim() ?? "");
                if (string.IsNullOrEmpty(title)) continue;

                // Image: handle lazy-loaded images (data-src / data-lazy-src)
                var imgNode = anchor.SelectSingleNode(".//img");
                var imgSrc = imgNode?.GetAttributeValue("data-lazy-src", null)
                    ?? imgNode?.GetAttributeValue("data-src", null)
                    ?? imgNode?.GetAttributeValue("src", null);
                if (imgSrc != null && !imgSrc.StartsWith("http"))
                    imgSrc = BaseUrl + imgSrc;

                // Date: scan all text nodes in the anchor
                string? dateRaw = null;
                foreach (var node in anchor.DescendantsAndSelf())
                {
                    if (node.NodeType != HtmlNodeType.Text) continue;
                    var text = WebUtility.HtmlDecode(node.InnerText).Trim();
                    if (string.IsNullOrWhiteSpace(text)) continue;
                    // Date tokens look like "15/05" or "15/05/2026" or "15. 5."
                    if (System.Text.RegularExpressions.Regex.IsMatch(text, @"\d{1,2}[/\.]\d{1,2}"))
                    {
                        dateRaw = text;
                        break;
                    }
                }

                var datumOd = ParseDate(dateRaw);
                if (datumOd is null) continue;

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
                logger.LogWarning(ex, "CvernovkaScraper: error parsing event item");
            }
        }

        // Deduplicate by SourceUrl (multiple selectors may match the same anchor)
        events = events.DistinctBy(e => e.SourceUrl).ToList();

        logger.LogInformation("CvernovkaScraper: scraped {Count} events", events.Count);
        return events;
    }

    // Parses "15/05 piatok 22:00" → "2026-05-15"
    private static string? ParseDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var token = raw.Trim().Split(' ')[0];
        var parts = token.Split('/');
        if (parts.Length != 2) return null;
        if (!int.TryParse(parts[0], out var day)) return null;
        if (!int.TryParse(parts[1], out var month)) return null;
        if (day < 1 || day > 31 || month < 1 || month > 12) return null;

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
