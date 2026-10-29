namespace Casagres.API.Models.Dtos.Pronostico;

public class PronosticoResponse
{
    public string? Mes { get; set; }

    public int CantidadProductos { get; set; }

    public List<ProductoPronosticoDto> Productos { get; set; } = new();
}

public class ProductoPronosticoDto
{
    public string Referencia { get; set; } = string.Empty;

    public double Pronostico { get; set; }
}
