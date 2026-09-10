namespace Casagres.API;

public interface IGraphService
{
    Task<List<object>> ObtenerArchivosAsync();

    Task<List<(string Id, string Nombre, DateTimeOffset UltimaModificacion)>> ObtenerEstadoArchivosAsync();

    Task ProbarGraphAsync();
}
