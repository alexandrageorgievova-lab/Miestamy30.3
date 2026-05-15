namespace Miestamy30._3.Services.Scrapers;

public class ScrapedEvent
{
    public string Nazov { get; set; } = string.Empty;
    public string? Popis { get; set; }
    public string DatumOd { get; set; } = string.Empty;
    public string? DatumDo { get; set; }
    public string? Adresa { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public string? ImageUrl { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public string? VenueNazov { get; set; }
}
