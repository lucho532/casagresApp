using Casagres.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Casagres.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PipelineController : ControllerBase
{
    private readonly PipelineService _pipelineService;
    private readonly GraphService _graphService;
    private readonly PipelineEstadoService _estadoService;
    public PipelineController(
    PipelineService pipelineService,
    GraphService graphService,
    PipelineEstadoService estadoService)
    {
        _pipelineService = pipelineService;
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
    public async Task<IActionResult> Ejecutar()
    {
        try
        {
            await _pipelineService.EjecutarPipeline();

            return Ok(new
            {
                estado = "OK",
                mensaje = "Pipeline ejecutado correctamente."
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
    public async Task<IActionResult> Actualizar()
    {
        try
        {
            await _pipelineService.EjecutarPipeline();

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