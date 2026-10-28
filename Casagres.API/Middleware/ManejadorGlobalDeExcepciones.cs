using Casagres.API.Services.Pronostico;
using Microsoft.AspNetCore.Diagnostics;

namespace Casagres.API.Middleware;

// Convierte las excepciones no controladas en respuestas JSON uniformes
// ({ mensaje }). Los datos del pronóstico vienen de archivos sincronizados
// desde OneDrive, así que que falte un archivo o no tenga datos es un caso
// esperado (todavía no se sincronizó) y se responde 404; cualquier otra
// cosa es un 500 genérico, sin exponer el mensaje interno de la excepción.
public class ManejadorGlobalDeExcepciones : IExceptionHandler
{
    private readonly ILogger<ManejadorGlobalDeExcepciones> _logger;

    public ManejadorGlobalDeExcepciones(ILogger<ManejadorGlobalDeExcepciones> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception excepcion,
        CancellationToken cancellationToken)
    {
        int estado;
        string mensaje;

        if (excepcion is FileNotFoundException or DatosNoEncontradosException)
        {
            estado = StatusCodes.Status404NotFound;
            mensaje = excepcion.Message;

            _logger.LogWarning(excepcion, "Datos no disponibles: {Mensaje}", excepcion.Message);
        }
        else
        {
            estado = StatusCodes.Status500InternalServerError;
            mensaje = "Ocurrió un error inesperado. Intenta de nuevo más tarde.";

            _logger.LogError(excepcion, "Excepción no controlada en {Ruta}", context.Request.Path);
        }

        context.Response.StatusCode = estado;

        await context.Response.WriteAsJsonAsync(new { mensaje }, cancellationToken);

        return true;
    }
}
