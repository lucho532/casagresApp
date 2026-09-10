using System.Net;
using System.Net.Mail;

namespace Casagres.API.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task EnviarAsync(string destinatario, string asunto, string cuerpoHtml)
    {
        var host = _configuration["Smtp:Host"];
        var puerto = int.Parse(_configuration["Smtp:Puerto"] ?? "587");
        var usuario = _configuration["Smtp:Usuario"];
        var password = _configuration["Smtp:Password"];
        var remitenteNombre = _configuration["Smtp:RemitenteNombre"] ?? "CASAGRES";

        using var mensaje = new MailMessage
        {
            From = new MailAddress(usuario ?? string.Empty, remitenteNombre),
            Subject = asunto,
            Body = cuerpoHtml,
            IsBodyHtml = true,
        };

        mensaje.To.Add(destinatario);

        using var cliente = new SmtpClient(host, puerto)
        {
            Credentials = new NetworkCredential(usuario, password),
            EnableSsl = true,
        };

        await cliente.SendMailAsync(mensaje);
    }
}
