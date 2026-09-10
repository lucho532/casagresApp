using Casagres.API.Data;
using Casagres.API.Models;
using Casagres.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Casagres.API.Tests.Services;

public class PasswordResetServiceTests
{
    private static CasagresDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<CasagresDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CasagresDbContext(opciones);
    }

    private static IConfiguration CrearConfiguracion()
    {
        var configuracion = new Mock<IConfiguration>();
        configuracion.Setup(c => c["Frontend:Url"]).Returns("http://localhost:5173");

        return configuracion.Object;
    }

    private static PasswordResetService CrearServicio(
        CasagresDbContext db, Mock<IEmailService> emailService) =>
        new(db, emailService.Object, CrearConfiguracion());

    // ============================================================
    // SolicitarResetAsync
    // ============================================================

    [Fact]
    public async Task SolicitarResetAsync_ConEmailExistenteYActivo_GeneraTokenYEnviaCorreo()
    {
        await using var db = CrearContexto();

        db.Usuarios.Add(new Usuario
        {
            UsuarioNombre = "jperez",
            PasswordHash = "hash",
            Nombre = "Juan Pérez",
            Email = "juan@ejemplo.com",
            Activo = true,
        });
        await db.SaveChangesAsync();

        var emailService = new Mock<IEmailService>();
        string? cuerpoEnviado = null;

        emailService
            .Setup(s => s.EnviarAsync("juan@ejemplo.com", It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string>((_, _, cuerpo) => cuerpoEnviado = cuerpo)
            .Returns(Task.CompletedTask);

        var servicio = CrearServicio(db, emailService);

        await servicio.SolicitarResetAsync("juan@ejemplo.com");

        var tokenGuardado = await db.PasswordResetTokens.SingleAsync();

        Assert.False(tokenGuardado.Usado);
        Assert.True(tokenGuardado.FechaExpiracion > DateTime.UtcNow);
        Assert.NotNull(cuerpoEnviado);
        Assert.Contains(tokenGuardado.Token, cuerpoEnviado);
        Assert.Contains("http://localhost:5173", cuerpoEnviado);
    }

    [Fact]
    public async Task SolicitarResetAsync_ConEmailInexistente_NoEnviaCorreoNiCreaToken()
    {
        await using var db = CrearContexto();
        var emailService = new Mock<IEmailService>();
        var servicio = CrearServicio(db, emailService);

        await servicio.SolicitarResetAsync("no-existe@ejemplo.com");

        Assert.Empty(db.PasswordResetTokens);
        emailService.Verify(
            s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task SolicitarResetAsync_ConUsuarioInactivo_NoEnviaCorreoNiCreaToken()
    {
        await using var db = CrearContexto();

        db.Usuarios.Add(new Usuario
        {
            UsuarioNombre = "jperez",
            PasswordHash = "hash",
            Email = "juan@ejemplo.com",
            Activo = false,
        });
        await db.SaveChangesAsync();

        var emailService = new Mock<IEmailService>();
        var servicio = CrearServicio(db, emailService);

        await servicio.SolicitarResetAsync("juan@ejemplo.com");

        Assert.Empty(db.PasswordResetTokens);
        emailService.Verify(
            s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    // ============================================================
    // RestablecerAsync
    // ============================================================

    [Fact]
    public async Task RestablecerAsync_ConTokenValido_CambiaLaContraseñaYMarcaElTokenComoUsado()
    {
        await using var db = CrearContexto();

        var usuario = new Usuario
        {
            UsuarioNombre = "jperez",
            PasswordHash = "hash-anterior",
            Activo = true,
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UsuarioId = usuario.Id,
            Token = "token-valido",
            FechaExpiracion = DateTime.UtcNow.AddMinutes(30),
            Usado = false,
        });
        await db.SaveChangesAsync();

        var servicio = CrearServicio(db, new Mock<IEmailService>());

        var resultado = await servicio.RestablecerAsync("token-valido", "clave-nueva");

        Assert.True(resultado);

        var usuarioActualizado = await db.Usuarios.SingleAsync();
        Assert.NotEqual("hash-anterior", usuarioActualizado.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("clave-nueva", usuarioActualizado.PasswordHash));

        var tokenActualizado = await db.PasswordResetTokens.SingleAsync();
        Assert.True(tokenActualizado.Usado);
    }

    [Fact]
    public async Task RestablecerAsync_ConTokenInexistente_DevuelveFalse()
    {
        await using var db = CrearContexto();
        var servicio = CrearServicio(db, new Mock<IEmailService>());

        var resultado = await servicio.RestablecerAsync("token-que-no-existe", "clave-nueva");

        Assert.False(resultado);
    }

    [Fact]
    public async Task RestablecerAsync_ConTokenYaUsado_DevuelveFalse()
    {
        await using var db = CrearContexto();

        var usuario = new Usuario { UsuarioNombre = "jperez", PasswordHash = "hash", Activo = true };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UsuarioId = usuario.Id,
            Token = "token-usado",
            FechaExpiracion = DateTime.UtcNow.AddMinutes(30),
            Usado = true,
        });
        await db.SaveChangesAsync();

        var servicio = CrearServicio(db, new Mock<IEmailService>());

        var resultado = await servicio.RestablecerAsync("token-usado", "clave-nueva");

        Assert.False(resultado);
    }

    [Fact]
    public async Task RestablecerAsync_ConTokenExpirado_DevuelveFalse()
    {
        await using var db = CrearContexto();

        var usuario = new Usuario { UsuarioNombre = "jperez", PasswordHash = "hash", Activo = true };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UsuarioId = usuario.Id,
            Token = "token-expirado",
            FechaExpiracion = DateTime.UtcNow.AddMinutes(-5),
            Usado = false,
        });
        await db.SaveChangesAsync();

        var servicio = CrearServicio(db, new Mock<IEmailService>());

        var resultado = await servicio.RestablecerAsync("token-expirado", "clave-nueva");

        Assert.False(resultado);
    }

    [Fact]
    public async Task RestablecerAsync_ConUsuarioInactivo_DevuelveFalse()
    {
        await using var db = CrearContexto();

        var usuario = new Usuario { UsuarioNombre = "jperez", PasswordHash = "hash", Activo = false };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UsuarioId = usuario.Id,
            Token = "token-valido",
            FechaExpiracion = DateTime.UtcNow.AddMinutes(30),
            Usado = false,
        });
        await db.SaveChangesAsync();

        var servicio = CrearServicio(db, new Mock<IEmailService>());

        var resultado = await servicio.RestablecerAsync("token-valido", "clave-nueva");

        Assert.False(resultado);
    }
}
