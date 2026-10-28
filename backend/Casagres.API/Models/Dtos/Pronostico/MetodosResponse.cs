namespace Casagres.API.Models.Dtos.Pronostico;

public class MetodosResponse
{
    public int CantidadProductos { get; set; }

    public List<ProductoMetodoDto> Productos { get; set; } = new();
}

public class ProductoMetodoDto
{
    public string Referencia { get; set; } = string.Empty;

    public string Metodo { get; set; } = string.Empty;

    public bool TieneHiperparametros { get; set; }

    public int NScoresAci { get; set; }

    public double AlphaAci { get; set; }
}
