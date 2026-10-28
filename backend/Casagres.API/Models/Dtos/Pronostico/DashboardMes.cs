namespace Casagres.API.Models.Dtos.Pronostico;

public class DashboardMes
{
    public string? Mes { get; set; }

    public int CantidadProductos { get; set; }

    public List<DashboardProducto> Productos { get; set; } = new();
}