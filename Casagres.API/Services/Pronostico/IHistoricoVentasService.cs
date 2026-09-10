using Casagres.API.Models.Dtos.Pronostico;

namespace Casagres.API.Services.Pronostico;

public interface IHistoricoVentasService
{
    HistoricoResponse Leer(string? referencia);
}
