using Casagres.API.Models.Dtos.Pronostico;

namespace Casagres.API.Services.Pronostico;

public interface IMetodosCsvService
{
    MetodosResponse Leer();
}
