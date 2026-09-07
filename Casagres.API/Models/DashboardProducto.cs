namespace Casagres.API.Models;

public class DashboardProducto
{
    public string Referencia { get; set; } = "";

    public double Pronostico { get; set; }

    public string? Metodo { get; set; }

    public bool? TieneHiperparametros { get; set; }

    public int? NScoresAci { get; set; }

    public double? AlphaAci { get; set; }

    public double? Inferior { get; set; }

    public double? Superior { get; set; }

    public double? HalfWidth { get; set; }
}