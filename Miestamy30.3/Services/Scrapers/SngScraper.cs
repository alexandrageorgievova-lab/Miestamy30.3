using System.Net;
using System.Text.Json;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Miestamy30._3.Services.Scrapers;

public class SngScraper(IHttpClientFactory httpFactory, ILogger<SngScraper> logger) : IEventScraper
{
    private const string EventsUrl = "https://sng.sk/sk/sng-bratislava/vystavy-programy?event_type=event";
    private const string EventDetailBase = "https://sng.sk/sk/sng-bratislava/podujatia/";
    private const string VenueNazov = "SNG";
    private const double Lat = 48.1408;
    private const double Lng = 17.1075;
    private const string Adresa = "Rázusovo nábrežie 2, Bratislava";

    public async Task<IReadOnlyList<ScrapedEvent>> ScrapeAsync(CancellationToken ct = default)
    {
        var client = httpFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

        string html;
        try { html = await client.GetStringAsync(EventsUrl, ct); }
        catch (Exception ex)
        {
            logger.LogError(ex, "SngScraper: failed to fetch {Url}", EventsUrl);
            return [];
        }

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // SNG uses Inertia.js — all event data is in the data-page JSON attribute on #app
        var appNode = doc.GetElementbyId("app");
        var rawDataPage = appNode?.GetAttributeValue("data-page", null);
        if (string.IsNullOrEmpty(rawDataPage))
        {
            logger.LogWarning("SngScraper: data-page attribute not found — site structure may have changed");
            return [];
        }

        JsonElement root;
        try { root = JsonSerializer.Deserialize<JsonElement>(WebUtility.HtmlDecode(rawDataPage)); }
        catch (Exception ex)
        {
            logger.LogError(ex, "SngScraper: failed to parse data-page JSON");
            return [];
        }

        if (!root.TryGetProperty("props", out var props) ||
            !props.TryGetProperty("events", out var eventsArr) ||
            eventsArr.ValueKind != JsonValueKind.Array)
        {
            logger.LogWarning("SngScraper: props.events not found in JSON");
            return [];
        }

        var events = new List<ScrapedEvent>();
        foreach (var ev in eventsArr.EnumerateArray())
        {
            try
            {
                var title = ev.TryGetProperty("title", out var t) ? t.GetString() : null;
                if (string.IsNullOrEmpty(title)) continue;

                var eventStart = ev.TryGetProperty("event_start", out var s) ? s.GetString() : null;
                if (!DateTime.TryParse(eventStart, out var startDate)) continue;
                if (startDate < DateTime.Today) continue;
                var datumOd = startDate.ToString("yyyy-MM-dd");

                var slug = ev.TryGetProperty("slug", out var sl) ? sl.GetString() : null;
                if (string.IsNullOrEmpty(slug)) continue;
                var sourceUrl = EventDetailBase + slug;

                string? imageUrl = null;
                if (ev.TryGetProperty("cover_image", out var cover) &&
                    cover.ValueKind == JsonValueKind.Object &&
                    cover.TryGetProperty("src", out var src))
                    imageUrl = src.GetString();

                string? popis = null;
                if (ev.TryGetProperty("perex", out var perexEl))
                    popis = perexEl.GetString();
                if (string.IsNullOrWhiteSpace(popis) && ev.TryGetProperty("intro_text", out var introEl))
                {
                    var raw = introEl.GetString() ?? "";
                    popis = System.Text.RegularExpressions.Regex.Replace(raw, "<[^>]*>", "").Trim();
                    if (popis.Length > 300) popis = popis[..300] + "…";
                }

                events.Add(new ScrapedEvent
                {
                    Nazov      = title,
                    Popis      = popis,
                    DatumOd    = datumOd,
                    Adresa     = Adresa,
                    Lat        = Lat,
                    Lng        = Lng,
                    ImageUrl   = imageUrl,
                    SourceUrl  = sourceUrl,
                    VenueNazov = VenueNazov,
                });
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "SngScraper: error parsing event");
            }
        }

        logger.LogInformation("SngScraper: scraped {Count} events", events.Count);
        return events;
    }
}
