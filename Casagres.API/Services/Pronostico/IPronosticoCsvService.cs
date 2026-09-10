using Casagres.API.Models.Dtos.Pronostico;

namespace Casagres.API.Services.Pronostico;

public interface IPronosticoCsvService
{
    PronosticoResponse Leer();
}
