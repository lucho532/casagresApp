using System.Net;
using Casagres.API.Services;
using Casagres.API.Tests.TestHelpers;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Casagres.API.Tests.Services;

public class EmailServiceTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactory = new();
    private readonly FakeHttpMessageHandler _handler = new();

    public EmailServiceTests()
    {
        _httpClientFactory
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(_handler));
    }

    private EmailService CrearServicio(
        string? apiKey = "xkeysib-clave-de-prueba",
        string? remitenteEmail = "ladrilleracasagres@gmail.com",
        string? remitenteNombre = "Casa Gres")
    {
        var configuracion = new Mock<IConfiguration>();
        configuracion.Setup(c => c["Brevo:ApiKey"]).Returns(apiKey);
        configuracion.Setup(c => c["Brevo:RemitenteEmail"]).Returns(remitenteEmail);
        configuracion.Setup(c => c["Brevo:RemitenteNombre"]).Returns(remitenteNombre);

        return new EmailService(configuracion.Object, _httpClientFactory.Object);
    }

    [Fact]
    public async Task EnviarAsync_ConRespuestaExitosa_EnviaLaSolicitudCorrectaALaApiDeBrevo()
    {
        _handler.EncolarRespuesta(HttpStatusCode.Created, """{ "messageId": "abc123" }""");

        var servicio = CrearServicio();

        await servicio.EnviarAsync(
            "destinatario@ejemplo.com", "Asunto de prueba", "<p>Cuerpo del correo</p>");

        Assert.Single(_handler.UrlsSolicitadas);
        Assert.Equal("https://api.brevo.com/v3/smtp/email", _handler.UrlsSolicitadas[0]);

        var solicitud = _handler.SolicitudesRecibidas[0];
        Assert.Equal("xkeysib-clave-de-prueba", solicitud.Headers.GetValues("api-key").Single());

        var cuerpo = _handler.CuerposSolicitados[0];
        Assert.Contains("\"email\":\"destinatario@ejemplo.com\"", cuerpo);
        Assert.Contains("\"email\":\"ladrilleracasagres@gmail.com\"", cuerpo);
        Assert.Contains("\"name\":\"Casa Gres\"", cuerpo);
        Assert.Contains("Asunto de prueba", cuerpo);
        Assert.Contains("Cuerpo del correo", cuerpo);
    }

    [Fact]
    public async Task EnviarAsync_CuandoBrevoRespondeConError_LanzaExcepcionConElDetalle()
    {
        _handler.EncolarRespuesta(
            HttpStatusCode.Unauthorized, """{ "code": "unauthorized", "message": "Key not found" }""");

        var servicio = CrearServicio();

        var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.EnviarAsync("destinatario@ejemplo.com", "Asunto", "<p>Cuerpo</p>"));

        Assert.Contains("401", excepcion.Message);
        Assert.Contains("Key not found", excepcion.Message);
    }

    [Fact]
    public async Task EnviarAsync_SinApiKeyConfigurada_LanzaExcepcionSinLlamarALaApi()
    {
        var servicio = CrearServicio(apiKey: null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.EnviarAsync("destinatario@ejemplo.com", "Asunto", "<p>Cuerpo</p>"));

        Assert.Empty(_handler.UrlsSolicitadas);
    }

    [Fact]
    public async Task EnviarAsync_SinRemitenteConfigurado_LanzaExcepcionSinLlamarALaApi()
    {
        var servicio = CrearServicio(remitenteEmail: null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.EnviarAsync("destinatario@ejemplo.com", "Asunto", "<p>Cuerpo</p>"));

        Assert.Empty(_handler.UrlsSolicitadas);
    }
}
