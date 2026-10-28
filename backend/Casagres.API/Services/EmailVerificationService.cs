using System.Security.Cryptography;
using Casagres.API.Data;
using Casagres.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Casagres.API.Services;

public class EmailVerificationService : IEmailVerificationService
{
    private static readonly TimeSpan DuracionToken = TimeSpan.FromHours(24);

    private readonly CasagresDbContext _db;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public EmailVerificationService(
        CasagresDbContext db,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _db = db;
        _emailService = emailService;
        _configuration = configuration;
    }

    // ============================================================
    // ENVIAR VERIFICACIÓN (al registrarse)
    // ============================================================

    public async Task EnviarCorreoDeVerificacionAsync(Usuario usuario)
    {
        if (string.IsNullOrWhiteSpace(usuario.Email))
            return;

        var token = GenerarToken();

        _db.EmailVerificationTokens.Add(new EmailVerificationToken
        {
            UsuarioId = usuario.Id,
            Token = token,
            FechaExpiracion = DateTime.UtcNow.Add(DuracionToken),
        });

        await _db.SaveChangesAsync();

        await EnviarCorreoAsync(usuario.Email, usuario.Nombre, token);
    }

    // ============================================================
    // REENVIAR VERIFICACIÓN
    // ============================================================
    //
    // Por seguridad, nunca informa si el correo existe, ya está
    // verificado, o no se encontró: el controlador siempre responde
    // el mismo mensaje genérico.

    public async Task ReenviarSiNoVerificadoAsync(string email)
    {
        email = email.Trim();

        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Email == email && u.Activo);

        if (usuario == null || usuario.EmailVerificado)
            return;

        await EnviarCorreoDeVerificacionAsync(usuario);
    }

    // ============================================================
    // VERIFICAR
    // ============================================================

    public async Task<bool> VerificarAsync(string token)
    {
        var registroToken = await _db.EmailVerificationTokens
            .FirstOrDefaultAsync(t => t.Token == token);

        if (!EsTokenValido(registroToken))
            return false;

        var usuario = await _db.Usuarios.FindAsync(registroToken!.UsuarioId);

        if (usuario == null)
            return false;

        usuario.EmailVerificado = true;
        registroToken.Usado = true;

        await _db.SaveChangesAsync();

        return true;
    }

    private static bool EsTokenValido(EmailVerificationToken? token) =>
        token != null &&
        !token.Usado &&
        token.FechaExpiracion >= DateTime.UtcNow;

    // ============================================================
    // CORREO
    // ============================================================

    private static string GenerarToken() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    private async Task EnviarCorreoAsync(string email, string? nombre, string token)
    {
        var urlFrontend = _configuration["Frontend:Url"]?.TrimEnd('/')
            ?? "http://localhost:5173";

        var enlace = $"{urlFrontend}/?verificarEmail={token}";

        await _emailService.EnviarAsync(
            email,
            "Confirma tu correo en CASAGRES",
            ConstruirCuerpoCorreo(nombre, enlace));
    }

    private static string ConstruirCuerpoCorreo(string? nombre, string enlace) =>
        $"""
        <p>Hola {nombre ?? "usuario"},</p>
        <p>Gracias por registrarte en CASAGRES. Confirma tu correo para poder iniciar sesión.</p>
        <p><a href="{enlace}">Haz clic aquí para confirmar tu correo</a></p>
        <p>Este enlace vence en 24 horas.</p>
        """;
}
