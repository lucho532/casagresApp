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
    public void ObtenerPronostico_CuandoFaltaConfiguracion_Devuelve500()
    {
        _pronosticoService.Setup(s => s.Leer())
            .Throws(new InvalidOperationException("No está configurada la ruta de datos."));

        var resultado = Assert.IsType<ObjectResult>(CrearController().ObtenerPronostico());

        Assert.Equal(500, resultado.StatusCode);
    }

    [Fact]
    public void ObtenerPronostico_CuandoNoExisteElArchivo_Devuelve404()
    {
        _pronosticoService.Setup(s => s.Leer())
            .Throws(new FileNotFoundException("No se encontró el archivo de pronóstico.", "ruta.csv"));

        Assert.IsType<NotFoundObjectResult>(CrearController().ObtenerPronostico());
    }

    [Fact]
    public void ObtenerPronostico_ConErrorInesperado_Devuelve500()
    {
        _pronosticoService.Setup(s => s.Leer()).Throws(new Exception("boom"));

        var resultado = Assert.IsType<ObjectResult>(CrearController().ObtenerPronostico());

        Assert.Equal(500, resultado.StatusCode);
    }

    // ============================================================
    // ObtenerPronosticoIntervalos
    // ============================================================

    [Fact]
    public void ObtenerPronosticoIntervalos_CuandoNoExisteElArchivo_Devuelve404()
    {
        _intervalosService.Setup(s => s.Leer())
            .Throws(new FileNotFoundException("No se encontró el archivo de intervalos."));

        Assert.IsType<NotFoundObjectResult>(CrearController().ObtenerPronosticoIntervalos());
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
    public void ObtenerMetodos_CuandoNoExisteElArchivo_Devuelve404()
    {
        _metodosService.Setup(s => s.Leer())
            .Throws(new FileNotFoundException("No se encontró el archivo de métodos."));

        Assert.IsType<NotFoundObjectResult>(CrearController().ObtenerMetodos());
    }

    // ============================================================
    // ObtenerDashboard
    // ============================================================

    [Fact]
    public void ObtenerDashboard_CuandoElServicioTieneExito_DevuelveOk()
    {
        _dashboardService.Setup(s => s.Leer()).Returns(new DashboardRespuesta());

        Assert.IsType<OkObjectResult>(CrearController().ObtenerDashboard());
    }

    [Fact]
    public void ObtenerDashboard_CuandoFaltaUnArchivo_Devuelve404()
    {
        _dashboardService.Setup(s => s.Leer())
            .Throws(new FileNotFoundException("No se encontró pronostico.csv."));

        Assert.IsType<NotFoundObjectResult>(CrearController().ObtenerDashboard());
    }

    [Fact]
    public void ObtenerDashboard_CuandoNoHayDatos_Devuelve404()
    {
        _dashboardService.Setup(s => s.Leer())
            .Throws(new DatosNoEncontradosException("pronostico.csv no contiene datos."));

        Assert.IsType<NotFoundObjectResult>(CrearController().ObtenerDashboard());
    }

    [Fact]
    public void ObtenerDashboard_ConFalloDeConfiguracion_Devuelve500()
    {
        _dashboardService.Setup(s => s.Leer())
            .Throws(new InvalidOperationException("No está configurada la ruta de datos."));

        var resultado = Assert.IsType<ObjectResult>(CrearController().ObtenerDashboard());

        Assert.Equal(500, resultado.StatusCode);
    }

    // ============================================================
    // ObtenerHistorico
    // ============================================================

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

    [Fact]
    public void ObtenerHistorico_ConReferenciaInexistente_Devuelve404()
    {
        _historicoService.Setup(s => s.Leer(It.IsAny<string?>()))
            .Throws(new DatosNoEncontradosException("No se encontró la referencia 'NO-EXISTE'."));

        Assert.IsType<NotFoundObjectResult>(CrearController().ObtenerHistorico("NO-EXISTE"));
    }
}
