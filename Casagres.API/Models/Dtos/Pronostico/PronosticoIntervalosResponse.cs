namespace Casagres.API.Models.Dtos.Pronostico;

public class PronosticoIntervalosResponse
{
    public string? Mes { get; set; }

    public int CantidadProductos { get; set; }

    public List<ProductoIntervaloDto> Productos { get; set; } = new();
}

public class ProductoIntervaloDto
{
    public string Referencia { get; set; } = string.Empty;

    public string Metodo { get; set; } = string.Empty;

    public double Prediccion { get; set; }

    public double Inferior { get; set; }

    public double Superior { get; set; }

    public double HalfWidth { get; set; }

    public double AlphaAci { get; set; }
}
