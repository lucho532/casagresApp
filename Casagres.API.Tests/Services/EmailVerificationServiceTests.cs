using Casagres.API.Data;
using Casagres.API.Models;
using Casagres.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Casagres.API.Tests.Services;

public class EmailVerificationServiceTests
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

    private static EmailVerificationService CrearServicio(
        CasagresDbContext db, Mock<IEmailService> emailService) =>
        new(db, emailService.Object, CrearConfiguracion());

    // ============================================================
    // EnviarCorreoDeVerificacionAsync
    // ============================================================

    [Fact]
    public async Task EnviarCorreoDeVerificacionAsync_ConUsuarioValido_GeneraTokenYEnviaCorreo()
    {
        await using var db = CrearContexto();

        var usuario = new Usuario
        {
            UsuarioNombre = "jperez",
            PasswordHash = "hash",
            Nombre = "Juan Pérez",
            Email = "juan@ejemplo.com",
            Activo = true,
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        var emailService = new Mock<IEmailService>();
        string? cuerpoEnviado = null;

        emailService
            .Setup(s => s.EnviarAsync("juan@ejemplo.com", It.IsAny<string>(), It.IsAny<string>()))
            .Callback<string, string, string>((_, _, cuerpo) => cuerpoEnviado = cuerpo)
            .Returns(Task.CompletedTask);

        var servicio = CrearServicio(db, emailService);

        await servicio.EnviarCorreoDeVerificacionAsync(usuario);

        var tokenGuardado = await db.EmailVerificationTokens.SingleAsync();

        Assert.False(tokenGuardado.Usado);
        Assert.True(tokenGuardado.FechaExpiracion > DateTime.UtcNow);
        Assert.NotNull(cuerpoEnviado);
        Assert.Contains(tokenGuardado.Token, cuerpoEnviado);
        Assert.Contains("verificarEmail=", cuerpoEnviado);
    }

    [Fact]
    public async Task EnviarCorreoDeVerificacionAsync_SinEmail_NoHaceNada()
    {
        await using var db = CrearContexto();

        var usuario = new Usuario { UsuarioNombre = "jperez", PasswordHash = "hash", Email = null };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        var emailService = new Mock<IEmailService>();
        var servicio = CrearServicio(db, emailService);

        await servicio.EnviarCorreoDeVerificacionAsync(usuario);

        Assert.Empty(db.EmailVerificationTokens);
        emailService.Verify(
            s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    // ============================================================
    // ReenviarSiNoVerificadoAsync
    // ============================================================

    [Fact]
    public async Task ReenviarSiNoVerificadoAsync_ConUsuarioNoVerificado_EnviaElCorreo()
    {
        await using var db = CrearContexto();

        db.Usuarios.Add(new Usuario
        {
            UsuarioNombre = "jperez",
            PasswordHash = "hash",
            Email = "juan@ejemplo.com",
            Activo = true,
            EmailVerificado = false,
        });
        await db.SaveChangesAsync();

        var emailService = new Mock<IEmailService>();
        var servicio = CrearServicio(db, emailService);

        await servicio.ReenviarSiNoVerificadoAsync("juan@ejemplo.com");

        Assert.Single(db.EmailVerificationTokens);
        emailService.Verify(
            s => s.EnviarAsync("juan@ejemplo.com", It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task ReenviarSiNoVerificadoAsync_ConUsuarioYaVerificado_NoEnviaNada()
    {
        await using var db = CrearContexto();

        db.Usuarios.Add(new Usuario
        {
            UsuarioNombre = "jperez",
            PasswordHash = "hash",
            Email = "juan@ejemplo.com",
            Activo = true,
            EmailVerificado = true,
        });
        await db.SaveChangesAsync();

        var emailService = new Mock<IEmailService>();
        var servicio = CrearServicio(db, emailService);

        await servicio.ReenviarSiNoVerificadoAsync("juan@ejemplo.com");

        Assert.Empty(db.EmailVerificationTokens);
        emailService.Verify(
            s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ReenviarSiNoVerificadoAsync_ConEmailInexistente_NoEnviaNadaNiLanzaExcepcion()
    {
        await using var db = CrearContexto();
        var emailService = new Mock<IEmailService>();
        var servicio = CrearServicio(db, emailService);

        await servicio.ReenviarSiNoVerificadoAsync("no-existe@ejemplo.com");

        emailService.Verify(
            s => s.EnviarAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    // ============================================================
    // VerificarAsync
    // ============================================================

    [Fact]
    public async Task VerificarAsync_ConTokenValido_MarcaElUsuarioComoVerificadoYElTokenComoUsado()
    {
        await using var db = CrearContexto();

        var usuario = new Usuario
        {
            UsuarioNombre = "jperez",
            PasswordHash = "hash",
            Activo = true,
            EmailVerificado = false,
        };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        db.EmailVerificationTokens.Add(new EmailVerificationToken
        {
            UsuarioId = usuario.Id,
            Token = "token-valido",
            FechaExpiracion = DateTime.UtcNow.AddHours(1),
            Usado = false,
        });
        await db.SaveChangesAsync();

        var servicio = CrearServicio(db, new Mock<IEmailService>());

        var resultado = await servicio.VerificarAsync("token-valido");

        Assert.True(resultado);

        var usuarioActualizado = await db.Usuarios.SingleAsync();
        Assert.True(usuarioActualizado.EmailVerificado);

        var tokenActualizado = await db.EmailVerificationTokens.SingleAsync();
        Assert.True(tokenActualizado.Usado);
    }

    [Fact]
    public async Task VerificarAsync_ConTokenInexistente_DevuelveFalse()
    {
        await using var db = CrearContexto();
        var servicio = CrearServicio(db, new Mock<IEmailService>());

        var resultado = await servicio.VerificarAsync("token-que-no-existe");

        Assert.False(resultado);
    }

    [Fact]
    public async Task VerificarAsync_ConTokenYaUsado_DevuelveFalse()
    {
        await using var db = CrearContexto();

        var usuario = new Usuario { UsuarioNombre = "jperez", PasswordHash = "hash", Activo = true };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        db.EmailVerificationTokens.Add(new EmailVerificationToken
        {
            UsuarioId = usuario.Id,
            Token = "token-usado",
            FechaExpiracion = DateTime.UtcNow.AddHours(1),
            Usado = true,
        });
        await db.SaveChangesAsync();

        var servicio = CrearServicio(db, new Mock<IEmailService>());

        var resultado = await servicio.VerificarAsync("token-usado");

        Assert.False(resultado);
    }

    [Fact]
    public async Task VerificarAsync_ConTokenExpirado_DevuelveFalse()
    {
        await using var db = CrearContexto();

        var usuario = new Usuario { UsuarioNombre = "jperez", PasswordHash = "hash", Activo = true };
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();

        db.EmailVerificationTokens.Add(new EmailVerificationToken
        {
            UsuarioId = usuario.Id,
            Token = "token-expirado",
            FechaExpiracion = DateTime.UtcNow.AddHours(-1),
            Usado = false,
        });
        await db.SaveChangesAsync();

        var servicio = CrearServicio(db, new Mock<IEmailService>());

        var resultado = await servicio.VerificarAsync("token-expirado");

        Assert.False(resultado);
    }
}
