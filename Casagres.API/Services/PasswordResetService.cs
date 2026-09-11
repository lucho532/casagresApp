using System.Security.Cryptography;
using Casagres.API.Data;
using Casagres.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Casagres.API.Services;

public class PasswordResetService : IPasswordResetService
{
    private static readonly TimeSpan DuracionToken = TimeSpan.FromHours(1);

    private readonly CasagresDbContext _db;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public PasswordResetService(
        CasagresDbContext db,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _db = db;
        _emailService = emailService;
        _configuration = configuration;
    }

    // ============================================================
    // SOLICITAR RESET
    // ============================================================
    //
    // Por seguridad, este método nunca informa si el correo existe o
    // no en el sistema: si no hay un usuario activo con ese correo,
    // simplemente no hace nada (el controlador siempre responde el
    // mismo mensaje genérico al cliente).

    public async Task SolicitarResetAsync(string email)
    {
        email = email.Trim();

        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Email == email && u.Activo);

        if (usuario == null)
            return;

        var token = GenerarToken();

        _db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UsuarioId = usuario.Id,
            Token = token,
            FechaExpiracion = DateTime.UtcNow.Add(DuracionToken),
        });

        await _db.SaveChangesAsync();

        await EnviarCorreoDeResetAsync(email, usuario.Nombre, token);
    }

    private static string GenerarToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    private async Task EnviarCorreoDeResetAsync(string email, string? nombre, string token)
    {
        var urlFrontend = _configuration["Frontend:Url"]?.TrimEnd('/')
            ?? "http://localhost:5173";

        var enlace = $"{urlFrontend}/?token={token}";

        await _emailService.EnviarAsync(
            email,
            "Restablece tu contraseña de CASAGRES",
            ConstruirCuerpoCorreo(nombre, enlace));
    }

    private static string ConstruirCuerpoCorreo(string? nombre, string enlace) =>
        $"""
        <p>Hola {nombre ?? "usuario"},</p>
        <p>Recibimos una solicitud para restablecer tu contraseña de CASAGRES.</p>
        <p><a href="{enlace}">Haz clic aquí para definir una nueva contraseña</a></p>
        <p>Este enlace vence en 1 hora. Si no solicitaste este cambio, puedes ignorar este correo.</p>
        """;

    // ============================================================
    // RESTABLECER CONTRASEÑA
    // ============================================================

    public async Task<bool> RestablecerAsync(string token, string nuevaPassword)
    {
        var registroToken = await _db.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.Token == token);

        if (!EsTokenValido(registroToken))
            return false;

        var usuario = await _db.Usuarios.FindAsync(registroToken!.UsuarioId);

        if (usuario == null || !usuario.Activo)
            return false;

        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(nuevaPassword);
        registroToken.Usado = true;

        await _db.SaveChangesAsync();

        return true;
    }

    private static bool EsTokenValido(PasswordResetToken? token) =>
        token != null &&
        !token.Usado &&
        token.FechaExpiracion >= DateTime.UtcNow;
}
