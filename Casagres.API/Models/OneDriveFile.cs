namespace Casagres.API.Models;

public class OneDriveFile
{
    public string Nombre { get; set; } = string.Empty;

    public string Ruta { get; set; } = string.Empty;

    public string Id { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public long Tamaño { get; set; }
}