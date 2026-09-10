using System.Net;
using Casagres.API.Tests.TestHelpers;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Casagres.API.Tests;

public class GraphServiceTests
{
    private readonly Mock<IMicrosoftAuthService> _authService = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
    private readonly FakeHttpMessageHandler _handler = new();

    private const string RespuestaCarpetaRaiz = """
        {
            "name": "INFORMES DE VENTAS",
            "id": "carpeta-1",
            "parentReference": { "driveId": "drive-1" }
        }
        """;

    public GraphServiceTests()
    {
        _authService.Setup(a => a.ObtenerTokenAsync()).ReturnsAsync("token-de-prueba");
        _httpClientFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(_handler));
    }

    private GraphService CrearServicio(
        string? urlCompartida = "https://1drv.ms/f/carpeta-compartida",
        string? carpetaDatos = null)
    {
        var configuracion = new Mock<IConfiguration>();
        configuracion.Setup(c => c["OneDrive:CarpetaCompartidaUrl"]).Returns(urlCompartida);
        configuracion.Setup(c => c["Rutas:CarpetaDatos"]).Returns(carpetaDatos);

        return new GraphService(_authService.Object, _httpClientFactory.Object, configuracion.Object);
    }

    // ============================================================
    // ObtenerArchivosAsync
    // ============================================================

    [Fact]
    public async Task ObtenerArchivosAsync_SinUrlCompartidaConfigurada_LanzaExcepcion()
    {
        var servicio = CrearServicio(urlCompartida: null);

        await Assert.ThrowsAsync<Exception>(servicio.ObtenerArchivosAsync);
    }

    [Fact]
    public async Task ObtenerArchivosAsync_DevuelveSoloLosArchivosNoLasCarpetas()
    {
        _handler.EncolarRespuesta(HttpStatusCode.OK, RespuestaCarpetaRaiz);
        _handler.EncolarRespuesta(HttpStatusCode.OK, """
            {
                "value": [
                    { "id": "1", "name": "informe.xlsx", "file": {} },
                    { "id": "2", "name": "subcarpeta", "folder": {} }
                ]
            }
            """);

        var archivos = await CrearServicio().ObtenerArchivosAsync();

        var archivo = Assert.Single(archivos);
        dynamic dinamico = archivo;
        Assert.Equal("informe.xlsx", (string)dinamico.nombre);
        Assert.Equal("INFORMES DE VENTAS", (string)dinamico.carpeta);
    }

    [Fact]
    public async Task ObtenerArchivosAsync_RecorreTodasLasPaginasDeResultados()
    {
        _handler.EncolarRespuesta(HttpStatusCode.OK, RespuestaCarpetaRaiz);
        _handler.EncolarRespuesta(HttpStatusCode.OK, """
            {
                "value": [ { "id": "1", "name": "informe1.xlsx", "file": {} } ],
                "@odata.nextLink": "https://graph.microsoft.com/v1.0/pagina2"
            }
            """);
        _handler.EncolarRespuesta(HttpStatusCode.OK, """
            {
                "value": [ { "id": "2", "name": "informe2.xlsx", "file": {} } ]
            }
            """);

        var archivos = await CrearServicio().ObtenerArchivosAsync();

        Assert.Equal(2, archivos.Count);
    }

    [Fact]
    public async Task ObtenerArchivosAsync_CuandoGraphRespondeConError_LanzaExcepcion()
    {
        _handler.EncolarRespuesta(HttpStatusCode.Unauthorized, "{\"error\":\"token inválido\"}");

        await Assert.ThrowsAsync<Exception>(CrearServicio().ObtenerArchivosAsync);
    }

    // ============================================================
    // ObtenerEstadoArchivosAsync
    // ============================================================

    [Fact]
    public async Task ObtenerEstadoArchivosAsync_DevuelveIdNombreYFechaDeCadaArchivo()
    {
        _handler.EncolarRespuesta(HttpStatusCode.OK, RespuestaCarpetaRaiz);
        _handler.EncolarRespuesta(HttpStatusCode.OK, """
            {
                "value": [
                    {
                        "id": "1",
                        "name": "informe.xlsx",
                        "file": {},
                        "lastModifiedDateTime": "2025-06-01T12:00:00Z"
                    }
                ]
            }
            """);

        var archivos = await CrearServicio().ObtenerEstadoArchivosAsync();

        var archivo = Assert.Single(archivos);
        Assert.Equal("1", archivo.Id);
        Assert.Equal("informe.xlsx", archivo.Nombre);
        Assert.Equal(
            new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero),
            archivo.UltimaModificacion);
    }

    [Fact]
    public async Task ObtenerEstadoArchivosAsync_IgnoraLasCarpetas()
    {
        _handler.EncolarRespuesta(HttpStatusCode.OK, RespuestaCarpetaRaiz);
        _handler.EncolarRespuesta(HttpStatusCode.OK, """
            {
                "value": [ { "id": "1", "name": "subcarpeta", "folder": {} } ]
            }
            """);

        var archivos = await CrearServicio().ObtenerEstadoArchivosAsync();

        Assert.Empty(archivos);
    }

    [Fact]
    public async Task ObtenerEstadoArchivosAsync_IgnoraArchivosSinFechaDeModificacionValida()
    {
        _handler.EncolarRespuesta(HttpStatusCode.OK, RespuestaCarpetaRaiz);
        _handler.EncolarRespuesta(HttpStatusCode.OK, """
            {
                "value": [
                    {
                        "id": "1",
                        "name": "informe.xlsx",
                        "file": {},
                        "lastModifiedDateTime": "fecha-invalida"
                    }
                ]
            }
            """);

        var archivos = await CrearServicio().ObtenerEstadoArchivosAsync();

        Assert.Empty(archivos);
    }

    // ============================================================
    // ProbarGraphAsync (descarga de archivos a disco)
    // ============================================================

    [Fact]
    public async Task ProbarGraphAsync_CuandoElArchivoDestinoEstaAbiertoEnOtroPrograma_LanzaExcepcionConMensajeClaro()
    {
        using var carpetaDatos = new CarpetaDatosTemporal();

        var carpetaInformes = Path.Combine(carpetaDatos.Ruta, "informes");
        Directory.CreateDirectory(carpetaInformes);

        var rutaArchivo = Path.Combine(carpetaInformes, "informe.xlsx");

        // Simula que alguien tiene el archivo abierto en Excel: lo
        // bloqueamos en modo exclusivo antes de que GraphService intente
        // sobrescribirlo.
        await using var bloqueo = new FileStream(
            rutaArchivo, FileMode.Create, FileAccess.Write, FileShare.None);

        _handler.EncolarRespuesta(HttpStatusCode.OK, RespuestaCarpetaRaiz);
        _handler.EncolarRespuesta(HttpStatusCode.OK, """
            {
                "value": [ { "id": "1", "name": "informe.xlsx", "file": {} } ]
            }
            """);
        _handler.EncolarRespuesta(HttpStatusCode.OK, "contenido de prueba");

        var servicio = CrearServicio(carpetaDatos: carpetaDatos.Ruta);

        var excepcion = await Assert.ThrowsAsync<IOException>(servicio.ProbarGraphAsync);

        Assert.Contains("abierto en otro programa", excepcion.Message);
    }
}
