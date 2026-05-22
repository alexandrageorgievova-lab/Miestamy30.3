namespace Miestamy30._3.Models.Dtos;

public class MiestoDetailDto
{
    public int Id { get; set; }
    public string Nazov { get; set; } = string.Empty;
    public string? Adresa { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public string? Popis { get; set; }
    public string? WebUrl { get; set; }
    public string? ImageUrl { get; set; }
    public List<string> Kategorie { get; set; } = [];
    public Dictionary<string, List<string>> Filtre { get; set; } = [];
}

public class MiestoSummaryDto
{
    public int Id { get; set; }
    public string Nazov { get; set; } = string.Empty;
    public string HlavnaKategoria { get; set; } = string.Empty;
    public double? Lat { get; set; }
    public double? Lng { get; set; }
}

public class FilterRowDto
{
    public string KategoriaNazov { get; set; } = string.Empty;
    public string FilterNazov { get; set; } = string.Empty;
}

public class KategoriaNazovDto
{
    public string Nazov { get; set; } = string.Empty;
}
