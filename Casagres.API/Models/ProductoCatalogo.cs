namespace Casagres.API.Models;

public record ProductoCatalogo
{
    public required string Referencia { get; init; }

    public string Descripcion { get; init; } = "";

    public string Marca { get; init; } = "";

    public string Linea { get; init; } = "";

    public string Grupo { get; init; } = "";

    public string Clase { get; init; } = "";

    public string Planta { get; init; } = "";
}
