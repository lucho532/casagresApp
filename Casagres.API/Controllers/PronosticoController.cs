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
    private readonly IPronosticoCsvService _pronosticoService;
    private readonly IPronosticoIntervalosCsvService _intervalosService;
    private readonly IMetodosCsvService _metodosService;
    private readonly IDashboardPronosticoService _dashboardService;
    private readonly IHistoricoVentasService _historicoService;

    public PronosticoController(
        IPronosticoCsvService pronosticoService,
        IPronosticoIntervalosCsvService intervalosService,
        IMetodosCsvService metodosService,
        IDashboardPronosticoService dashboardService,
        IHistoricoVentasService historicoService)
    {
        _pronosticoService = pronosticoService;
        _intervalosService = intervalosService;
        _metodosService = metodosService;
        _dashboardService = dashboardService;
        _historicoService = historicoService;
    }

    [HttpGet("pronostico")]
    public IActionResult ObtenerPronostico()
    {
        return Ok(_pronosticoService.Leer());
    }

    [HttpGet("pronostico-intervalos")]
    public IActionResult ObtenerPronosticoIntervalos()
    {
        return Ok(_intervalosService.Leer());
    }

    [HttpGet("metodos")]
    public IActionResult ObtenerMetodos()
    {
        return Ok(_metodosService.Leer());
    }

    [HttpGet("dashboard")]
    public IActionResult ObtenerDashboard()
    {
        return Ok(_dashboardService.Leer());
    }

    [HttpGet("historico")]
    public IActionResult ObtenerHistorico([FromQuery] string? referencia = null)
    {
        return Ok(_historicoService.Leer(referencia));
    }
}
