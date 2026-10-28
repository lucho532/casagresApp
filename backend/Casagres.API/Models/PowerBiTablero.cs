namespace Casagres.API.Models;

public class PowerBiTablero
{
    public long Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
