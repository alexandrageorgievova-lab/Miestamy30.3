namespace Miestamy30._3.Models.Dtos;

public class PodujatieDetailDto
{
    public int Id { get; set; }
    public string Nazov { get; set; } = string.Empty;
    public string? Popis { get; set; }
    public string DatumOd { get; set; } = string.Empty;
    public string? DatumDo { get; set; }
    public string? Adresa { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public string? MiestoNazov { get; set; }
    public string? ImageUrl { get; set; }
    public string? SourceUrl { get; set; }
    public List<string> Typy { get; set; } = [];
    public List<string> Filtre { get; set; } = [];
}

public class PodujatieSummaryDto
{
    public int Id { get; set; }
    public string Nazov { get; set; } = string.Empty;
    public string DatumOd { get; set; } = string.Empty;
    public string? DatumDo { get; set; }
    public string? Adresa { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public string HlavnyTyp { get; set; } = string.Empty;
}

public class TypNazovDto
{
    public string Nazov { get; set; } = string.Empty;
}

public class EventFilterNazovDto
{
    public string Nazov { get; set; } = string.Empty;
}
