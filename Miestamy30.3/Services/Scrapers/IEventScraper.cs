namespace Miestamy30._3.Services.Scrapers;

public interface IEventScraper
{
    Task<IReadOnlyList<ScrapedEvent>> ScrapeAsync(CancellationToken ct = default);
}
