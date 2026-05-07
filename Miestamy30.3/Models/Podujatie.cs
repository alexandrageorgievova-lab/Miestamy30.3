namespace Miestamy30._3.Models;

public class Podujatie
{
    public int Id { get; set; }
    public string Nazov { get; set; } = string.Empty;
    public string? Popis { get; set; }
    public string DatumOd { get; set; } = string.Empty;
    public string? DatumDo { get; set; }
    public string? Adresa { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public int? MiestoId { get; set; }
}
