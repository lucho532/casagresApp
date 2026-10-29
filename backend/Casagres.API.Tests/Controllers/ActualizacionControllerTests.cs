using Casagres.API.Controllers;
using Casagres.API.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Casagres.API.Tests.Controllers;

public class ActualizacionControllerTests
{
    private readonly Mock<IActualizacionService> _actualizacionService = new();
    private readonly Mock<IGraphService> _graphService = new();
    private readonly ActualizacionEstadoService _estadoService = new();

    private ActualizacionController CrearController() => new(
        _actualizacionService.Object,
        _graphService.Object,
        _estadoService);

    // ============================================================
    // Estado
    // ============================================================

    [Fact]
    public void Estado_DevuelveElEstadoActualDelServicio()
    {
        _estadoService.Iniciar();
        _estadoService.Actualizar("Procesando...", 40);

        var resultado = Assert.IsType<OkObjectResult>(CrearController().Estado());

        Assert.NotNull(resultado.Value);
    }

    // ============================================================
    // Ejecutar
    // ============================================================

    [Fact]
    public async Task Ejecutar_CuandoElServicioTieneExito_DevuelveOk()
    {
        _actualizacionService.Setup(s => s.EjecutarActualizacion(3)).Returns(Task.CompletedTask);

        var resultado = await CrearController().Ejecutar(3);

        Assert.IsType<OkObjectResult>(resultado);
    }

    [Fact]
    public async Task Ejecutar_CuandoElServicioLanzaExcepcion_Devuelve500()
    {
        _actualizacionService
            .Setup(s => s.EjecutarActualizacion(It.IsAny<int>()))
            .ThrowsAsync(new Exception("boom"));

        var resultado = Assert.IsType<ObjectResult>(await CrearController().Ejecutar());

        Assert.Equal(500, resultado.StatusCode);
    }

    // ============================================================
    // ProbarOneDrive
    // ============================================================

    [Fact]
    public async Task ProbarOneDrive_CuandoElServicioTieneExito_DevuelveOk()
    {
        _graphService.Setup(s => s.ObtenerArchivosAsync()).ReturnsAsync(new List<object> { new() });

        Assert.IsType<OkObjectResult>(await CrearController().ProbarOneDrive());
    }

    [Fact]
    public async Task ProbarOneDrive_CuandoElServicioLanzaExcepcion_Devuelve500()
    {
        _graphService.Setup(s => s.ObtenerArchivosAsync())
            .ThrowsAsync(new Exception("Error accediendo a OneDrive"));

        var resultado = Assert.IsType<ObjectResult>(await CrearController().ProbarOneDrive());

        Assert.Equal(500, resultado.StatusCode);
    }

    // ============================================================
    // ProbarArchivo
    // ============================================================

    [Fact]
    public void ProbarArchivo_SiempreDevuelveOk()
    {
        Assert.IsType<OkObjectResult>(CrearController().ProbarArchivo());
    }

    // ============================================================
    // Actualizar
    // ============================================================

    [Fact]
    public async Task Actualizar_CuandoElServicioTieneExito_DevuelveOk()
    {
        _actualizacionService.Setup(s => s.EjecutarActualizacion(It.IsAny<int>())).Returns(Task.CompletedTask);

        Assert.IsType<OkObjectResult>(await CrearController().Actualizar());
    }

    [Fact]
    public async Task Actualizar_CuandoYaHayUnaEnCurso_Devuelve409()
    {
        _actualizacionService
            .Setup(s => s.EjecutarActualizacion(It.IsAny<int>()))
            .ThrowsAsync(new InvalidOperationException("Ya hay una actualización en curso."));

        var resultado = Assert.IsType<ConflictObjectResult>(await CrearController().Actualizar());

        Assert.NotNull(resultado.Value);
    }

    [Fact]
    public async Task Actualizar_ConErrorInesperado_Devuelve500()
    {
        _actualizacionService
            .Setup(s => s.EjecutarActualizacion(It.IsAny<int>()))
            .ThrowsAsync(new Exception("boom"));

        var resultado = Assert.IsType<ObjectResult>(await CrearController().Actualizar());

        Assert.Equal(500, resultado.StatusCode);
    }
}
