namespace Casagres.API.Services;

public interface IActualizacionService
{
    Task EjecutarActualizacion(int horizonte = 1);
}
