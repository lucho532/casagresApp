namespace Casagres.API.Models;

public class DashboardRespuesta
{
    public string? Mes { get; set; }

    public int CantidadProductos { get; set; }

    public List<DashboardProducto> Productos { get; set; } = new();
}