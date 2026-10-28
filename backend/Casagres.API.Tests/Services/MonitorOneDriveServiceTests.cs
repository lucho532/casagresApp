using System.Reflection;
using Casagres.API.Services;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Casagres.API.Tests.Services;

// La lógica que realmente interesa probar (RevisarActualizacion, CrearHuella)
// es privada, porque el único punto de entrada público es el ciclo de vida
// de BackgroundService (con una espera fija de 10s y un bucle infinito).
// Se invoca por reflection para poder probarla de forma rápida y aislada.
public class MonitorOneDriveServiceTests : IDisposable
{
    private readonly Mock<IGraphService> _graphService = new();
    private readonly Mock<IActualizacionService> _actualizacionService = new();

    private readonly string _carpetaTemporal =
        Path.Combine(Path.GetTempPath(), "casagres-tests-" + Guid.NewGuid());

    private string RutaEstado => Path.Combine(_carpetaTemporal, "estado_actualizacion.json");

    public void Dispose()
    {
        if (Directory.Exists(_carpetaTemporal))
        {
            Directory.Delete(_carpetaTemporal, recursive: true);
        }
    }

    private MonitorOneDriveService CrearServicio()
    {
        var configuracion = new Mock<IConfiguration>();
        configuracion.Setup(c => c["Rutas:CarpetaDatos"]).Returns(_carpetaTemporal);

        return new MonitorOneDriveService(
            _graphService.Object, _actualizacionService.Object, configuracion.Object);
    }

    private static Task InvocarRevisarActualizacion(MonitorOneDriveService servicio)
    {
        var metodo = typeof(MonitorOneDriveService)
            .GetMethod("RevisarActualizacion", BindingFlags.NonPublic | BindingFlags.Instance)!;

        return (Task)metodo.Invoke(servicio, new object[] { CancellationToken.None })!;
    }

    private static string InvocarCrearHuella(
        MonitorOneDriveService servicio,
        List<(string Id, string Nombre, DateTimeOffset UltimaModificacion)> archivos)
    {
        var metodo = typeof(MonitorOneDriveService)
            .GetMethod("CrearHuella", BindingFlags.NonPublic | BindingFlags.Instance)!;

        return (string)metodo.Invoke(servicio, new object[] { archivos })!;
    }

    [Fact]
    public async Task RevisarActualizacion_SinArchivosEnOneDrive_NoEjecutaLaActualizacion()
    {
        _graphService
            .Setup(g => g.ObtenerEstadoArchivosAsync())
            .ReturnsAsync(new List<(string, string, DateTimeOffset)>());

        await InvocarRevisarActualizacion(CrearServicio());

        _actualizacionService.Verify(a => a.EjecutarActualizacion(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RevisarActualizacion_SinCambiosRespectoAlEstadoAnterior_NoEjecutaLaActualizacion()
    {
        var archivos = new List<(string, string, DateTimeOffset)>
        {
            ("1", "informe.xlsx", new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero))
        };

        _graphService.Setup(g => g.ObtenerEstadoArchivosAsync()).ReturnsAsync(archivos);

        var servicio = CrearServicio();

        // Guardamos de antemano la huella que corresponde a estos mismos
        // archivos, simulando que ya se procesaron en una revisión anterior.
        Directory.CreateDirectory(_carpetaTemporal);
        File.WriteAllText(RutaEstado, InvocarCrearHuella(servicio, archivos));

        await InvocarRevisarActualizacion(servicio);

        _actualizacionService.Verify(a => a.EjecutarActualizacion(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RevisarActualizacion_ConCambios_EjecutaLaActualizacionYGuardaElNuevoEstado()
    {
        var archivos = new List<(string, string, DateTimeOffset)>
        {
            ("1", "informe.xlsx", new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero))
        };

        _graphService.Setup(g => g.ObtenerEstadoArchivosAsync()).ReturnsAsync(archivos);
        _actualizacionService
            .Setup(a => a.EjecutarActualizacion(It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        await InvocarRevisarActualizacion(CrearServicio());

        _actualizacionService.Verify(a => a.EjecutarActualizacion(It.IsAny<int>()), Times.Once);
        Assert.True(File.Exists(RutaEstado));
    }

    [Fact]
    public async Task RevisarActualizacion_CuandoLaActualizacionFalla_NoGuardaElEstadoNuevo()
    {
        var archivos = new List<(string, string, DateTimeOffset)>
        {
            ("1", "informe.xlsx", new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero))
        };

        _graphService.Setup(g => g.ObtenerEstadoArchivosAsync()).ReturnsAsync(archivos);
        _actualizacionService
            .Setup(a => a.EjecutarActualizacion(It.IsAny<int>()))
            .ThrowsAsync(new Exception("falló el pipeline"));

        // RevisarActualizacion atrapa la excepción internamente, así que no
        // se debe propagar: solo nos interesa que no se guarde el estado.
        await InvocarRevisarActualizacion(CrearServicio());

        Assert.False(File.Exists(RutaEstado));
    }
}
