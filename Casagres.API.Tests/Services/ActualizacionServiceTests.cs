using Casagres.API.Services;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Casagres.API.Tests.Services;

public class ActualizacionServiceTests
{
    private readonly Mock<IGraphService> _graphService = new();
    private readonly Mock<IProductoService> _productoService = new();
    private readonly ActualizacionEstadoService _estadoService = new();

    public ActualizacionServiceTests()
    {
        _graphService.Setup(g => g.ProbarGraphAsync()).Returns(Task.CompletedTask);
    }

    private ActualizacionService CrearServicio(
        string? pythonEjecutable = @"C:\python\python.exe",
        string? pythonProyecto = @"C:\proyecto",
        string? carpetaDatos = @"C:\datos")
    {
        var configuracion = new Mock<IConfiguration>();
        configuracion.Setup(c => c["Python:Ejecutable"]).Returns(pythonEjecutable);
        configuracion.Setup(c => c["Python:Proyecto"]).Returns(pythonProyecto);
        configuracion.Setup(c => c["Rutas:CarpetaDatos"]).Returns(carpetaDatos);

        return new ActualizacionService(
            configuracion.Object, _graphService.Object, _estadoService, _productoService.Object);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task EjecutarActualizacion_ConHorizonteMenorOIgualACero_LanzaArgumentException(int horizonte)
    {
        var servicio = CrearServicio();

        await Assert.ThrowsAsync<ArgumentException>(() => servicio.EjecutarActualizacion(horizonte));

        // No debe haberse tocado nada más: ni el estado, ni OneDrive.
        _graphService.Verify(g => g.ProbarGraphAsync(), Times.Never);
    }

    [Fact]
    public async Task EjecutarActualizacion_SinPythonEjecutableConfigurado_LanzaInvalidOperationException()
    {
        var servicio = CrearServicio(pythonEjecutable: null);

        var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.EjecutarActualizacion());

        Assert.Equal("No se configuró Python:Ejecutable en appsettings.json.", excepcion.Message);
    }

    [Fact]
    public async Task EjecutarActualizacion_SinPythonProyectoConfigurado_LanzaInvalidOperationException()
    {
        var servicio = CrearServicio(pythonProyecto: null);

        var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.EjecutarActualizacion());

        Assert.Equal("No se configuró Python:Proyecto en appsettings.json.", excepcion.Message);
    }

    [Fact]
    public async Task EjecutarActualizacion_SinCarpetaDeDatosConfigurada_LanzaInvalidOperationException()
    {
        var servicio = CrearServicio(carpetaDatos: null);

        var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.EjecutarActualizacion());

        Assert.Equal("No se configuró Rutas:CarpetaDatos en appsettings.json.", excepcion.Message);
    }

    [Fact]
    public async Task EjecutarActualizacion_SinConfiguracionValida_NoLlegaADescargarDeOneDrive()
    {
        // La validación de configuración ocurre antes de tocar OneDrive.
        var servicio = CrearServicio(pythonEjecutable: null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => servicio.EjecutarActualizacion());

        _graphService.Verify(g => g.ProbarGraphAsync(), Times.Never);
    }

    [Fact]
    public async Task EjecutarActualizacion_CuandoFallaPorScriptInexistente_MarcaElEstadoComoFallidoYPropagaLaExcepcion()
    {
        // Configuración "válida" mirando a rutas que no existen: la etapa de
        // descarga de OneDrive (mockeada) tiene éxito, pero al intentar
        // ejecutar el primer script Python este no existe en disco.
        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<FileNotFoundException>(
            () => servicio.EjecutarActualizacion());

        Assert.Contains("No se encontró el script", excepcion.Message);

        _graphService.Verify(g => g.ProbarGraphAsync(), Times.Once);
        Assert.False(_estadoService.Ejecutando);
        Assert.Equal("Error durante la actualización.", _estadoService.Estado);
        Assert.Equal(excepcion.Message, _estadoService.Error);
    }

    [Fact]
    public async Task EjecutarActualizacion_CuandoYaHayUnaEjecucionEnCurso_LanzaInvalidOperationException()
    {
        var señalParaContinuar = new TaskCompletionSource();
        _graphService.Setup(g => g.ProbarGraphAsync()).Returns(señalParaContinuar.Task);

        var servicio = CrearServicio();

        // Primera ejecución: queda "colgada" dentro de ProbarGraphAsync
        // hasta que liberemos la señal, simulando trabajo en curso.
        var primeraEjecucion = servicio.EjecutarActualizacion();

        // Segunda ejecución concurrente: debe rechazarse de inmediato
        // porque el semáforo ya está tomado.
        var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.EjecutarActualizacion());

        Assert.Equal("Ya hay una actualización en curso.", excepcion.Message);

        // Liberamos la primera ejecución para que el test no quede colgado.
        señalParaContinuar.SetResult();
        await Assert.ThrowsAsync<FileNotFoundException>(() => primeraEjecucion);
    }
}
