using Casagres.API.Models.Dtos.Pronostico;

namespace Casagres.API.Services.Pronostico;

public interface IPronosticoIntervalosCsvService
{
    PronosticoIntervalosResponse Leer();
}
