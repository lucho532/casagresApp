using Casagres.API.Models;
using Casagres.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Casagres.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.ConAccesoADatos)]
public class ActualizacionController : ControllerBase
{
    private readonly IActualizacionService _actualizacionService;
    private readonly IGraphService _graphService;
    private readonly ActualizacionEstadoService _estadoService;
    public ActualizacionController(
    IActualizacionService actualizacionService,
    IGraphService graphService,
    ActualizacionEstadoService estadoService)
    {
        _actualizacionService = actualizacionService;
        _graphService = graphService;
        _estadoService = estadoService;
    }
    [HttpGet("estado")]
    public IActionResult Estado()
    {
        return Ok(new
        {
            ejecutando = _estadoService.Ejecutando,
            estado = _estadoService.Estado,
            progreso = _estadoService.Progreso,
            inicio = _estadoService.Inicio,
            fin = _estadoService.Fin,
            ultimaActualizacion = _estadoService.UltimaActualizacion,
            error = _estadoService.Error

        });
    }
    [HttpPost("ejecutar")]
    public async Task<IActionResult> Ejecutar(int horizonte = 1)
    {
        try
        {
            await _actualizacionService.EjecutarActualizacion(horizonte);

            return Ok(new
            {
                estado = "OK",
                mensaje = "Actualización ejecutada correctamente."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                estado = "ERROR",
                mensaje = ex.Message
            });
        }
    }

    [HttpGet("probar-onedrive")]
    public async Task<IActionResult> ProbarOneDrive()
    {
        try
        {
            var archivos =
                await _graphService.ObtenerArchivosAsync();

            return Ok(new
            {
                mensaje =
                    "Archivos obtenidos correctamente desde OneDrive",

                cantidad = archivos.Count,

                archivos
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                mensaje = "Error accediendo a OneDrive",
                error = ex.Message
            });
        }
    }

    [HttpGet("probar-archivo")]
    public IActionResult ProbarArchivo()
    {
        return Ok(new
        {
            mensaje =
                "Este endpoint queda reservado para pruebas de archivos."
        });
    }

    [HttpPost("actualizar")]
    public async Task<IActionResult> Actualizar(int horizonte = 1)
    {
        try
        {
            await _actualizacionService.EjecutarActualizacion(horizonte);

            return Ok(new
            {
                estado = "OK",
                mensaje = "Datos actualizados correctamente."
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                estado = "EN_CURSO",
                mensaje = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                estado = "ERROR",
                mensaje = "Error durante la actualización.",
                detalle = ex.Message
            });
        }
    }
}