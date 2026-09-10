using Casagres.API.Models;
using Casagres.API.Services;
using Casagres.API.Services.Pronostico;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Casagres.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.ConAccesoADatos)]
public class PronosticoController : ControllerBase
{
    private readonly IExcelExportService _excelExportService;
    private readonly IPronosticoCsvService _pronosticoService;
    private readonly IPronosticoIntervalosCsvService _intervalosService;
    private readonly IMetodosCsvService _metodosService;
    private readonly IDashboardPronosticoService _dashboardService;
    private readonly IHistoricoVentasService _historicoService;

    public PronosticoController(
        IExcelExportService excelExportService,
        IPronosticoCsvService pronosticoService,
        IPronosticoIntervalosCsvService intervalosService,
        IMetodosCsvService metodosService,
        IDashboardPronosticoService dashboardService,
        IHistoricoVentasService historicoService)
    {
        _excelExportService = excelExportService;
        _pronosticoService = pronosticoService;
        _intervalosService = intervalosService;
        _metodosService = metodosService;
        _dashboardService = dashboardService;
        _historicoService = historicoService;
    }

    [HttpGet("generar-csv")]
    public async Task<IActionResult> GenerarCsv()
    {
        try
        {
            var rutasCsv = await _excelExportService.GenerarCsvVentas();

            return Ok(new
            {
                mensaje = "CSV generados correctamente.",
                archivos = rutasCsv.Select(Path.GetFileName),
                rutas = rutasCsv
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                mensaje = "Error al generar los CSV.",
                error = ex.Message
            });
        }
    }

    [HttpGet("pronostico")]
    public IActionResult ObtenerPronostico()
    {
        try
        {
            return Ok(_pronosticoService.Leer());
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { mensaje = ex.Message });
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message, ruta = ex.FileName });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                mensaje = "Error al obtener el pronóstico.",
                error = ex.Message
            });
        }
    }

    [HttpGet("pronostico-intervalos")]
    public IActionResult ObtenerPronosticoIntervalos()
    {
        try
        {
            return Ok(_intervalosService.Leer());
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { mensaje = ex.Message });
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message, ruta = ex.FileName });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                mensaje = "Error al obtener los intervalos del pronóstico.",
                error = ex.Message
            });
        }
    }

    [HttpGet("metodos")]
    public IActionResult ObtenerMetodos()
    {
        try
        {
            return Ok(_metodosService.Leer());
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { mensaje = ex.Message });
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { mensaje = ex.Message, ruta = ex.FileName });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                mensaje = "Error al obtener los métodos por serie.",
                error = ex.Message
            });
        }
    }

    [HttpGet("dashboard")]
    public IActionResult ObtenerDashboard()
    {
        try
        {
            return Ok(_dashboardService.Leer());
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { mensaje = ex.Message });
        }
        catch (Exception ex) when (ex is FileNotFoundException or DatosNoEncontradosException)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                mensaje = "Error al construir los datos del dashboard.",
                error = ex.Message
            });
        }
    }

    [HttpGet("historico")]
    public IActionResult ObtenerHistorico([FromQuery] string? referencia = null)
    {
        try
        {
            return Ok(_historicoService.Leer(referencia));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { mensaje = ex.Message });
        }
        catch (Exception ex) when (ex is FileNotFoundException or DatosNoEncontradosException)
        {
            return NotFound(new { mensaje = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                mensaje = "Error al obtener el histórico de ventas.",
                error = ex.Message
            });
        }
    }
}
