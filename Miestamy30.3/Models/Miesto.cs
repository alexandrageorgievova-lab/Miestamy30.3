namespace Miestamy30._3.Models;

public class Miesto
{
    public int Id { get; set; }
    public string Nazov { get; set; } = string.Empty;
    public string? Adresa { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public string? Popis { get; set; }
    public string? WebUrl { get; set; }
}
