namespace Casagres.API.Models.Dtos.Pronostico;

public class DashboardRespuesta
{
    // Meses de pronóstico a futuro (lo que ya consumía el resto de la app:
    // el selector de "horizonte" en Inicio/Demanda futura/Decisiones/
    // Tendencias siempre toma el primero de esta lista). No mezclar acá el
    // histórico: rompería ese selector, que asume que el primer mes es el
    // horizonte más próximo, no un mes ya pasado.
    public List<DashboardMes> Meses { get; set; } = new();

    // Backtest causal de meses ya observados (qué habría predicho el
    // modelo en ese momento), usado solo para pintar el rango
    // mínimo/máximo también en la zona histórica de la gráfica.
    public List<DashboardMes> MesesHistoricos { get; set; } = new();
}