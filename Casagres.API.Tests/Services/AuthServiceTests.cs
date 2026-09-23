using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using Casagres.API.Data;
using Casagres.API.Models;
using Casagres.API.Services;
using Casagres.API.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Casagres.API.Tests.Services;

public class AuthServiceTests
{
    private const string ClaveJwtDePrueba = "clave-de-prueba-suficientemente-larga-para-hmacsha256";

    private static CasagresDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<CasagresDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CasagresDbContext(opciones);
    }

    private static IConfiguration CrearConfiguracion(string? claveJwt = ClaveJwtDePrueba)
    {
        var configuracion = new Mock<IConfiguration>();
        configuracion.Setup(c => c["Jwt:Key"]).Returns(claveJwt);

        return configuracion.Object;
    }

    private static IHttpClientFactory CrearFabricaHttpClient(HttpMessageHandler? handler = null)
    {
        var fabrica = new Mock<IHttpClientFactory>();
        fabrica.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => handler != null ? new HttpClient(handler) : new HttpClient());

        return fabrica.Object;
    }

    private static IEmailVerificationService CrearEmailVerificationServiceSimulado() =>
        new Mock<IEmailVerificationService>().Object;

    // El envío de verificación al registrarse corre en un scope nuevo (ver
    // AuthService.EnviarVerificacionSinFallarElRegistroAsync), así que el
    // scope factory de prueba tiene que resolver este mismo
    // IEmailVerificationService para que las verificaciones de Moq sigan
    // funcionando.
    private static IServiceScopeFactory CrearFabricaDeScopes(
        IEmailVerificationService emailVerificationService)
    {
        var servicios = new ServiceCollection();
        servicios.AddSingleton(emailVerificationService);

        return servicios.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    private static AuthService CrearServicio(
        CasagresDbContext db,
        string? claveJwt = ClaveJwtDePrueba,
        IEmailVerificationService? emailVerificationService = null,
        HttpMessageHandler? httpHandler = null)
    {
        var servicioEmail = emailVerificationService ?? CrearEmailVerificationServiceSimulado();

        return new(
            db,
            CrearConfiguracion(claveJwt),
            CrearFabricaHttpClient(httpHandler),
            servicioEmail,
            CrearFabricaDeScopes(servicioEmail));
    }

    // ============================================================
    // LoginAsync
    // ============================================================

    [Fact]
    public async Task LoginAsync_ConCredencialesValidas_DevuelveUnTokenConLosClaimsDelUsuario()
    {
        await using var db = CrearContexto();

        db.Usuarios.Add(new Usuario
        {
            UsuarioNombre = "jperez@ejemplo.com",
            Email = "jperez@ejemplo.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("clave123"),
            Nombre = "Juan Pérez",
            Rol = "admin",
            Activo = true,
            EmailVerificado = true
        });
        await db.SaveChangesAsync();

        var servicio = CrearServicio(db);

        var token = await servicio.LoginAsync("jperez@ejemplo.com", "clave123");

        Assert.NotNull(token);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("jperez@ejemplo.com", jwt.Claims.Single(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal("admin", jwt.Claims.Single(c => c.Type == ClaimTypes.Role).Value);
        Assert.Equal("Juan Pérez", jwt.Claims.Single(c => c.Type == "nombre").Value);
    }

    [Fact]
    public async Task LoginAsync_ConEspaciosAlrededorDelCorreo_IgualEncuentraLaCuenta()
    {
        await using var db = CrearContexto();

        db.Usuarios.Add(new Usuario
        {
            UsuarioNombre = "jperez@ejemplo.com",
            Email = "jperez@ejemplo.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("clave123"),
            Activo = true,
            EmailVerificado = true
        });
        await db.SaveChangesAsync();

        var servicio = CrearServicio(db);

        var token = await servicio.LoginAsync("  jperez@ejemplo.com  ", "clave123");

        Assert.NotNull(token);
    }

    [Fact]
    public async Task LoginAsync_ConCorreoInexistente_DevuelveNull()
    {
        await using var db = CrearContexto();
        var servicio = CrearServicio(db);

        var token = await servicio.LoginAsync("no-existe@ejemplo.com", "clave123");

        Assert.Null(token);
    }

    [Fact]
    public async Task LoginAsync_ConUsuarioInactivo_DevuelveNull()
    {
        await using var db = CrearContexto();

        db.Usuarios.Add(new Usuario
        {
            UsuarioNombre = "jperez@ejemplo.com",
            Email = "jperez@ejemplo.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("clave123"),
            Activo = false
        });
        await db.SaveChangesAsync();

        var servicio = CrearServicio(db);

        var token = await servicio.LoginAsync("jperez@ejemplo.com", "clave123");

        Assert.Null(token);
    }

    [Fact]
    public async Task LoginAsync_ConContraseñaIncorrecta_DevuelveNull()
    {
        await using var db = CrearContexto();

        db.Usuarios.Add(new Usuario
        {
            UsuarioNombre = "jperez@ejemplo.com",
            Email = "jperez@ejemplo.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("clave-correcta"),
            Activo = true
        });
        await db.SaveChangesAsync();

        var servicio = CrearServicio(db);

        var token = await servicio.LoginAsync("jperez@ejemplo.com", "clave-incorrecta");

        Assert.Null(token);
    }

    [Fact]
    public async Task LoginAsync_ConEmailNoVerificado_LanzaEmailNoVerificadoException()
    {
        await using var db = CrearContexto();

        db.Usuarios.Add(new Usuario
        {
            UsuarioNombre = "jperez@ejemplo.com",
            Email = "jperez@ejemplo.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("clave123"),
            Activo = true,
            EmailVerificado = false
        });
        await db.SaveChangesAsync();

        var servicio = CrearServicio(db);

        await Assert.ThrowsAsync<EmailNoVerificadoException>(
            () => servicio.LoginAsync("jperez@ejemplo.com", "clave123"));
    }

    [Fact]
    public async Task LoginAsync_SinClaveJwtConfigurada_LanzaInvalidOperationException()
    {
        await using var db = CrearContexto();

        db.Usuarios.Add(new Usuario
        {
            UsuarioNombre = "jperez@ejemplo.com",
            Email = "jperez@ejemplo.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("clave123"),
            Activo = true,
            EmailVerificado = true
        });
        await db.SaveChangesAsync();

        var servicio = CrearServicio(db, claveJwt: null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => servicio.LoginAsync("jperez@ejemplo.com", "clave123"));
    }

    // ============================================================
    // RegistrarAsync
    // ============================================================

    [Fact]
    public async Task RegistrarAsync_ConDatosNuevos_CreaElUsuarioYDevuelveTrue()
    {
        await using var db = CrearContexto();
        var servicio = CrearServicio(db);

        var resultado = await servicio.RegistrarAsync(
            "clave123", "Nombre Apellido", "correo@ejemplo.com");

        Assert.True(resultado);

        var usuarioCreado = await db.Usuarios.SingleAsync(u => u.Email == "correo@ejemplo.com");
        Assert.Equal("Nombre Apellido", usuarioCreado.Nombre);
        Assert.True(usuarioCreado.Activo);
        Assert.Equal(Roles.Pendiente, usuarioCreado.Rol);
        Assert.False(usuarioCreado.EmailVerificado);
    }

    [Fact]
    public async Task RegistrarAsync_UsaElCorreoComoNombreDeUsuarioInterno()
    {
        await using var db = CrearContexto();
        var servicio = CrearServicio(db);

        await servicio.RegistrarAsync("clave123", "Nombre Apellido", "correo@ejemplo.com");

        var usuarioCreado = await db.Usuarios.SingleAsync(u => u.Email == "correo@ejemplo.com");
        Assert.Equal("correo@ejemplo.com", usuarioCreado.UsuarioNombre);
    }

    [Fact]
    public async Task RegistrarAsync_ConEspaciosAlrededorDeLosCampos_LosGuardaRecortados()
    {
        await using var db = CrearContexto();
        var servicio = CrearServicio(db);

        var resultado = await servicio.RegistrarAsync(
            "clave123", "  Nombre Apellido  ", "  correo@ejemplo.com  ");

        Assert.True(resultado);

        var usuarioCreado = await db.Usuarios.SingleAsync(u => u.Email == "correo@ejemplo.com");
        Assert.Equal("Nombre Apellido", usuarioCreado.Nombre);
        Assert.Equal("correo@ejemplo.com", usuarioCreado.Email);
    }

    [Fact]
    public async Task RegistrarAsync_ConDatosNuevos_EnviaElCorreoDeVerificacion()
    {
        await using var db = CrearContexto();

        var emailVerificationService = new Mock<IEmailVerificationService>();
        var servicio = CrearServicio(db, emailVerificationService: emailVerificationService.Object);

        await servicio.RegistrarAsync("clave123", "Nombre Apellido", "correo@ejemplo.com");

        emailVerificationService.Verify(
            s => s.EnviarCorreoDeVerificacionAsync(
                It.Is<Usuario>(u => u.Email == "correo@ejemplo.com")),
            Times.Once);
    }

    [Fact]
    public async Task RegistrarAsync_CuandoFallaElEnvioDelCorreo_DevuelveTrueIgual()
    {
        await using var db = CrearContexto();

        var emailVerificationService = new Mock<IEmailVerificationService>();
        emailVerificationService
            .Setup(s => s.EnviarCorreoDeVerificacionAsync(It.IsAny<Usuario>()))
            .ThrowsAsync(new Exception("SMTP no disponible"));

        var servicio = CrearServicio(db, emailVerificationService: emailVerificationService.Object);

        var resultado = await servicio.RegistrarAsync(
            "clave123", "Nombre Apellido", "correo@ejemplo.com");

        Assert.True(resultado);
        Assert.True(await db.Usuarios.AnyAsync(u => u.Email == "correo@ejemplo.com"));
    }

    [Fact]
    public async Task RegistrarAsync_GuardaLaContraseñaComoHashYNoEnTextoPlano()
    {
        await using var db = CrearContexto();
        var servicio = CrearServicio(db);

        await servicio.RegistrarAsync("clave123", "Nombre", "correo@ejemplo.com");

        var usuarioCreado = await db.Usuarios.SingleAsync(u => u.Email == "correo@ejemplo.com");
        Assert.NotEqual("clave123", usuarioCreado.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("clave123", usuarioCreado.PasswordHash));
    }

    [Fact]
    public async Task RegistrarAsync_ConEmailYaRegistrado_DevuelveFalse()
    {
        await using var db = CrearContexto();

        db.Usuarios.Add(new Usuario
        {
            UsuarioNombre = "correo@ejemplo.com",
            PasswordHash = "hash-existente",
            Email = "correo@ejemplo.com"
        });
        await db.SaveChangesAsync();

        var servicio = CrearServicio(db);

        var resultado = await servicio.RegistrarAsync(
            "clave123", "Nombre", "correo@ejemplo.com");

        Assert.False(resultado);
        Assert.Equal(1, await db.Usuarios.CountAsync());
    }

    // ============================================================
    // LoginConGoogleAsync
    // ============================================================

    [Fact]
    public async Task LoginConGoogleAsync_ConUsuarioNuevo_LoCreaConRolPendiente()
    {
        await using var db = CrearContexto();

        var handler = new FakeHttpMessageHandler().EncolarRespuesta(
            HttpStatusCode.OK,
            """{"email":"nuevo@ejemplo.com","name":"Nuevo Usuario"}""");

        var servicio = CrearServicio(db, httpHandler: handler);

        var token = await servicio.LoginConGoogleAsync("access-token-valido");

        Assert.NotNull(token);

        var usuarioCreado = await db.Usuarios.SingleAsync(u => u.Email == "nuevo@ejemplo.com");
        Assert.Equal(Roles.Pendiente, usuarioCreado.Rol);
        Assert.True(usuarioCreado.EmailVerificado);
    }

    [Fact]
    public async Task LoginConGoogleAsync_ConUsuarioExistente_NoLeCambiaElRolYaAprobado()
    {
        await using var db = CrearContexto();

        db.Usuarios.Add(new Usuario
        {
            UsuarioNombre = "juan@ejemplo.com",
            PasswordHash = "hash",
            Email = "juan@ejemplo.com",
            Rol = Roles.Admin,
            Activo = true,
        });
        await db.SaveChangesAsync();

        var handler = new FakeHttpMessageHandler().EncolarRespuesta(
            HttpStatusCode.OK,
            """{"email":"juan@ejemplo.com","name":"Juan"}""");

        var servicio = CrearServicio(db, httpHandler: handler);

        var token = await servicio.LoginConGoogleAsync("access-token-valido");

        Assert.NotNull(token);

        var usuario = await db.Usuarios.SingleAsync(u => u.Email == "juan@ejemplo.com");
        Assert.Equal(Roles.Admin, usuario.Rol);
    }

    [Fact]
    public async Task LoginConGoogleAsync_ConUsuarioNuevo_GuardaLaFotoDePerfil()
    {
        await using var db = CrearContexto();

        var handler = new FakeHttpMessageHandler().EncolarRespuesta(
            HttpStatusCode.OK,
            """{"email":"nuevo@ejemplo.com","name":"Nuevo Usuario","picture":"https://foto.ejemplo.com/nuevo.jpg"}""");

        var servicio = CrearServicio(db, httpHandler: handler);

        await servicio.LoginConGoogleAsync("access-token-valido");

        var usuarioCreado = await db.Usuarios.SingleAsync(u => u.Email == "nuevo@ejemplo.com");
        Assert.Equal("https://foto.ejemplo.com/nuevo.jpg", usuarioCreado.FotoUrl);
    }

    [Fact]
    public async Task LoginConGoogleAsync_ConUsuarioExistente_ActualizaLaFotoDePerfil()
    {
        await using var db = CrearContexto();

        db.Usuarios.Add(new Usuario
        {
            UsuarioNombre = "juan@ejemplo.com",
            PasswordHash = "hash",
            Email = "juan@ejemplo.com",
            Rol = Roles.Usuario,
            Activo = true,
            FotoUrl = "https://foto.ejemplo.com/vieja.jpg",
        });
        await db.SaveChangesAsync();

        var handler = new FakeHttpMessageHandler().EncolarRespuesta(
            HttpStatusCode.OK,
            """{"email":"juan@ejemplo.com","name":"Juan","picture":"https://foto.ejemplo.com/nueva.jpg"}""");

        var servicio = CrearServicio(db, httpHandler: handler);

        await servicio.LoginConGoogleAsync("access-token-valido");

        var usuario = await db.Usuarios.SingleAsync(u => u.Email == "juan@ejemplo.com");
        Assert.Equal("https://foto.ejemplo.com/nueva.jpg", usuario.FotoUrl);
    }

    [Fact]
    public async Task LoginConGoogleAsync_ConUsuarioExistenteYRespuestaSinFoto_NoLeBorraLaFotoQueYaTenia()
    {
        // Ejercita la misma ruta compartida que usa Microsoft (cuyo login no
        // se puede probar aquí directamente: valida el id_token contra el
        // endpoint real de descubrimiento OIDC de Azure, sin ningún punto de
        // inyección para simularlo). Microsoft nunca aporta "picture", así
        // que esto reproduce exactamente esa misma condición.
        await using var db = CrearContexto();

        db.Usuarios.Add(new Usuario
        {
            UsuarioNombre = "juan@ejemplo.com",
            PasswordHash = "hash",
            Email = "juan@ejemplo.com",
            Rol = Roles.Usuario,
            Activo = true,
            FotoUrl = "https://foto.ejemplo.com/google.jpg",
        });
        await db.SaveChangesAsync();

        var handler = new FakeHttpMessageHandler().EncolarRespuesta(
            HttpStatusCode.OK,
            """{"email":"juan@ejemplo.com","name":"Juan"}""");

        var servicio = CrearServicio(db, httpHandler: handler);

        await servicio.LoginConGoogleAsync("access-token-valido");

        var usuario = await db.Usuarios.SingleAsync(u => u.Email == "juan@ejemplo.com");
        Assert.Equal("https://foto.ejemplo.com/google.jpg", usuario.FotoUrl);
    }
}
