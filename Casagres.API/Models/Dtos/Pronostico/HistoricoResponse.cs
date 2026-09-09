namespace Casagres.API.Models.Dtos.Pronostico;

public class HistoricoResponse
{
    public string? Referencia { get; set; }

    public int CantidadMeses { get; set; }

    public List<HistoricoMensual> Historico { get; set; } = new();
}
