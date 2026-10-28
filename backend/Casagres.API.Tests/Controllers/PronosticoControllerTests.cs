using Casagres.API.Controllers;
using Casagres.API.Models.Dtos.Pronostico;
using Casagres.API.Services;
using Casagres.API.Services.Pronostico;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Casagres.API.Tests.Controllers;

public class PronosticoControllerTests
{
    private readonly Mock<IPronosticoCsvService> _pronosticoService = new();
    private readonly Mock<IPronosticoIntervalosCsvService> _intervalosService = new();
    private readonly Mock<IMetodosCsvService> _metodosService = new();
    private readonly Mock<IDashboardPronosticoService> _dashboardService = new();
    private readonly Mock<IHistoricoVentasService> _historicoService = new();

    private PronosticoController CrearController() => new(
        _pronosticoService.Object,
        _intervalosService.Object,
        _metodosService.Object,
        _dashboardService.Object,
        _historicoService.Object);

    // ============================================================
    // ObtenerPronostico
    // ============================================================

    [Fact]
    public void ObtenerPronostico_CuandoElServicioTieneExito_DevuelveOkConLaRespuesta()
    {
        var respuesta = new PronosticoResponse { Mes = "2025-01-01" };
        _pronosticoService.Setup(s => s.Leer()).Returns(respuesta);

        var resultado = Assert.IsType<OkObjectResult>(CrearController().ObtenerPronostico());

        Assert.Same(respuesta, resultado.Value);
    }

    [Fact]
    public void ObtenerPronosticoIntervalos_CuandoElServicioTieneExito_DevuelveOk()
    {
        _intervalosService.Setup(s => s.Leer()).Returns(new PronosticoIntervalosResponse());

        Assert.IsType<OkObjectResult>(CrearController().ObtenerPronosticoIntervalos());
    }

    // ============================================================
    // ObtenerMetodos
    // ============================================================

    [Fact]
    public void ObtenerMetodos_CuandoElServicioTieneExito_DevuelveOk()
    {
        _metodosService.Setup(s => s.Leer()).Returns(new MetodosResponse());

        Assert.IsType<OkObjectResult>(CrearController().ObtenerMetodos());
    }

    [Fact]
    public void ObtenerDashboard_CuandoElServicioTieneExito_DevuelveOk()
    {
        _dashboardService.Setup(s => s.Leer()).Returns(new DashboardRespuesta());

        Assert.IsType<OkObjectResult>(CrearController().ObtenerDashboard());
    }

    [Fact]
    public void ObtenerHistorico_CuandoElServicioTieneExito_DevuelveOk()
    {
        _historicoService.Setup(s => s.Leer(It.IsAny<string?>())).Returns(new HistoricoResponse());

        Assert.IsType<OkObjectResult>(CrearController().ObtenerHistorico("REF1"));
    }

    [Fact]
    public void ObtenerHistorico_PasaLaReferenciaRecibidaAlServicio()
    {
        _historicoService.Setup(s => s.Leer("REF1")).Returns(new HistoricoResponse());

        CrearController().ObtenerHistorico("REF1");

        _historicoService.Verify(s => s.Leer("REF1"), Times.Once);
    }

    // ============================================================
    // Errores: el controlador no los atrapa, los resuelve el manejador global
    // ============================================================

    [Fact]
    public void ObtenerPronostico_CuandoElServicioFalla_PropagaLaExcepcion()
    {
        _pronosticoService.Setup(s => s.Leer()).Throws(new FileNotFoundException("falta"));

        Assert.Throws<FileNotFoundException>(() => CrearController().ObtenerPronostico());
    }

    [Fact]
    public void ObtenerPronosticoIntervalos_CuandoElServicioFalla_PropagaLaExcepcion()
    {
        _intervalosService.Setup(s => s.Leer()).Throws(new FileNotFoundException("falta"));

        Assert.Throws<FileNotFoundException>(() => CrearController().ObtenerPronosticoIntervalos());
    }

    [Fact]
    public void ObtenerMetodos_CuandoElServicioFalla_PropagaLaExcepcion()
    {
        _metodosService.Setup(s => s.Leer()).Throws(new FileNotFoundException("falta"));

        Assert.Throws<FileNotFoundException>(() => CrearController().ObtenerMetodos());
    }

    [Fact]
    public void ObtenerDashboard_CuandoNoHayDatos_PropagaLaExcepcion()
    {
        _dashboardService.Setup(s => s.Leer())
            .Throws(new DatosNoEncontradosException("pronostico.csv no contiene datos."));

        Assert.Throws<DatosNoEncontradosException>(() => CrearController().ObtenerDashboard());
    }

    [Fact]
    public void ObtenerHistorico_ConReferenciaInexistente_PropagaLaExcepcion()
    {
        _historicoService.Setup(s => s.Leer(It.IsAny<string?>()))
            .Throws(new DatosNoEncontradosException("No se encontró la referencia 'NO-EXISTE'."));

        Assert.Throws<DatosNoEncontradosException>(() => CrearController().ObtenerHistorico("NO-EXISTE"));
    }
}
