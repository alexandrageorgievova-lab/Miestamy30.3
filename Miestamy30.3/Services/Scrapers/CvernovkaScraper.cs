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
        var anchors = doc.DocumentNode.SelectNodes("//a[contains(@href,'/program/') and .//h3 and .//img]");
        if (anchors is null) return events;

        foreach (var anchor in anchors)
        {
            try
            {
                var href = anchor.GetAttributeValue("href", "");
                if (string.IsNullOrEmpty(href) || href == ProgramUrl) continue;
                var sourceUrl = href.StartsWith("http") ? href : BaseUrl + href;

                var titleNode = anchor.SelectSingleNode(".//h3");
                var title = WebUtility.HtmlDecode(titleNode?.InnerText.Trim() ?? "");
                if (string.IsNullOrEmpty(title)) continue;

                var imgSrc = anchor.SelectSingleNode(".//img")?.GetAttributeValue("src", null);

                // Extract text nodes for date and description
                string? dateRaw = null;
                var descParts = new List<string>();
                bool afterH3 = false;
                foreach (var child in anchor.ChildNodes)
                {
                    if (child.Name == "h3") { afterH3 = true; continue; }
                    if (child.NodeType == HtmlNodeType.Text)
                    {
                        var text = WebUtility.HtmlDecode(child.InnerText).Trim();
                        if (string.IsNullOrWhiteSpace(text)) continue;
                        if (!afterH3) dateRaw = text;
                        else descParts.Add(text);
                    }
                }

                var datumOd = ParseDate(dateRaw);
                if (datumOd is null) continue;

                events.Add(new ScrapedEvent
                {
                    Nazov      = title,
                    Popis      = descParts.Count > 0 ? string.Join(" ", descParts) : null,
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
