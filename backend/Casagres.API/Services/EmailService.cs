using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Casagres.API.Services;

// Envía correo por la API HTTP de Brevo (antes se usaba SMTP directo contra
// Gmail). Se cambió porque la mayoría de plataformas de hosting (Render,
// Railway, y en general casi cualquier nube) bloquean el tráfico saliente
// a los puertos SMTP (25/465/587) para evitar abuso de spam -verificado
// directamente: la conexión a smtp.gmail.com:587 se cuelga en timeout
// desde dentro del contenedor-. La API HTTP de Brevo usa el puerto 443
// (HTTPS), que ninguna plataforma de este tipo bloquea.
public class EmailService : IEmailService
{
    private const string BrevoUrl = "https://api.brevo.com/v3/smtp/email";

    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public EmailService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    public async Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml)
    {
        var apiKey = _configuration["Brevo:ApiKey"];
        var remitenteEmail = _configuration["Brevo:RemitenteEmail"];
        var remitenteNombre = _configuration["Brevo:RemitenteNombre"] ?? "CASAGRES";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("No se configuró Brevo:ApiKey.");
        }

        if (string.IsNullOrWhiteSpace(remitenteEmail))
        {
            throw new InvalidOperationException("No se configuró Brevo:RemitenteEmail.");
        }

        var cuerpoSolicitud = new
        {
            sender = new { name = remitenteNombre, email = remitenteEmail },
            to = new[] { new { email = destinatario } },
            subject = asunto,
            htmlContent = cuerpoHtml,
        };

        using var solicitud = new HttpRequestMessage(HttpMethod.Post, BrevoUrl)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(cuerpoSolicitud), Encoding.UTF8, "application/json"),
        };

        solicitud.Headers.Add("api-key", apiKey);
        solicitud.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var cliente = _httpClientFactory.CreateClient();

        var respuesta = await cliente.SendAsync(solicitud);

        if (!respuesta.IsSuccessStatusCode)
        {
            var detalle = await respuesta.Content.ReadAsStringAsync();

            throw new InvalidOperationException(
                $"Brevo respondió con error ({(int)respuesta.StatusCode}): {detalle}");
        }
    }
}
