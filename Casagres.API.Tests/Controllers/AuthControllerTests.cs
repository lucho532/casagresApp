using System.Security.Claims;
using Casagres.API.Controllers;
using Casagres.API.Models;
using Casagres.API.Models.Dtos.Auth;
using Casagres.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Casagres.API.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authService = new();
    private readonly Mock<IPasswordResetService> _passwordResetService = new();
    private readonly Mock<IEmailVerificationService> _emailVerificationService = new();
    private readonly Mock<IFotoPerfilService> _fotoPerfilService = new();

    // El controlador resuelve IPasswordResetService/IEmailVerificationService
    // desde un scope nuevo para las tareas en segundo plano (ver
    // SolicitarResetSinFallarLaRespuestaAsync), así que el scope factory de
    // prueba tiene que devolver estos mismos mocks para que las
    // verificaciones de Moq sigan funcionando.
    private IServiceScopeFactory CrearFabricaDeScopes()
    {
        var servicios = new ServiceCollection();
        servicios.AddSingleton(_passwordResetService.Object);
        servicios.AddSingleton(_emailVerificationService.Object);

        return servicios.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    private AuthController CrearController() =>
        new(
            _authService.Object,
            _passwordResetService.Object,
            _emailVerificationService.Object,
            CrearFabricaDeScopes(),
            _fotoPerfilService.Object);

    private AuthController CrearControllerAutenticado(long usuarioId)
    {
        var controller = CrearController();

        var identidad = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()) },
            "TestAuth");

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identidad) }
        };

        return controller;
    }

    // ============================================================
    // Login
    // ============================================================

    [Fact]
    public async Task Login_ConCredencialesValidas_DevuelveOkConElToken()
    {
        _authService.Setup(s => s.LoginAsync("jperez@ejemplo.com", "clave123")).ReturnsAsync("un-token-jwt");

        var resultado = Assert.IsType<OkObjectResult>(
            await CrearController().Login(
                new LoginRequest { Email = "jperez@ejemplo.com", Password = "clave123" }));

        Assert.NotNull(resultado.Value);
    }

    [Fact]
    public async Task Login_ConCredencialesIncorrectas_Devuelve401()
    {
        _authService.Setup(s => s.LoginAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string?)null);

        var resultado = await CrearController().Login(
            new LoginRequest { Email = "jperez@ejemplo.com", Password = "mala-clave" });

        Assert.IsType<UnauthorizedObjectResult>(resultado);
    }

    [Theory]
    [InlineData("", "clave123")]
    [InlineData("jperez@ejemplo.com", "")]
    [InlineData(" ", " ")]
    public async Task Login_ConCamposFaltantes_Devuelve400SinLlamarAlServicio(string email, string password)
    {
        var resultado = await CrearController().Login(
            new LoginRequest { Email = email, Password = password });

        Assert.IsType<BadRequestObjectResult>(resultado);
        _authService.Verify(
            s => s.LoginAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Login_ConEmailNoVerificado_Devuelve401ConCodigoEspecifico()
    {
        _authService.Setup(s => s.LoginAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new EmailNoVerificadoException("jperez@ejemplo.com"));

        var resultado = Assert.IsType<UnauthorizedObjectResult>(
            await CrearController().Login(
                new LoginRequest { Email = "jperez@ejemplo.com", Password = "clave123" }));

        Assert.NotNull(resultado.Value);
    }

    // ============================================================
    // Perfil
    // ============================================================

    [Fact]
    public async Task Perfil_ConUsuarioAutenticadoYExistente_DevuelveSusDatosActuales()
    {
        _authService.Setup(s => s.ObtenerPorIdAsync(1)).ReturnsAsync(new Usuario
        {
            Id = 1,
            UsuarioNombre = "jperez",
            Nombre = "Juan Pérez",
            Email = "juan@ejemplo.com",
            Rol = Roles.Usuario,
            Activo = true,
        });

        var resultado = Assert.IsType<OkObjectResult>(
            await CrearControllerAutenticado(1).Perfil());

        Assert.NotNull(resultado.Value);
    }

    [Fact]
    public async Task Perfil_ConUsuarioQueYaNoExiste_Devuelve401()
    {
        _authService.Setup(s => s.ObtenerPorIdAsync(It.IsAny<long>())).ReturnsAsync((Usuario?)null);

        var resultado = await CrearControllerAutenticado(1).Perfil();

        Assert.IsType<UnauthorizedResult>(resultado);
    }

    [Fact]
    public async Task Perfil_SinClaimDeIdentificador_Devuelve401()
    {
        var controller = CrearController();

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };

        var resultado = await controller.Perfil();

        Assert.IsType<UnauthorizedResult>(resultado);
        _authService.Verify(s => s.ObtenerPorIdAsync(It.IsAny<long>()), Times.Never);
    }

    // ============================================================
    // Registro
    // ============================================================

    [Fact]
    public async Task Registro_ConDatosValidos_DevuelveOk()
    {
        _authService
            .Setup(s => s.RegistrarAsync("clave123", "Nombre", "correo@ejemplo.com"))
            .ReturnsAsync(true);

        var resultado = await CrearController().Registro(new RegistroRequest
        {
            Password = "clave123",
            Nombre = "Nombre",
            Email = "correo@ejemplo.com"
        });

        Assert.IsType<OkObjectResult>(resultado);
    }

    [Fact]
    public async Task Registro_ConCorreoYaExistente_Devuelve409()
    {
        _authService
            .Setup(s => s.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var resultado = await CrearController().Registro(new RegistroRequest
        {
            Password = "clave123",
            Nombre = "Nombre",
            Email = "correo@ejemplo.com"
        });

        Assert.IsType<ConflictObjectResult>(resultado);
    }

    [Fact]
    public async Task Registro_ConCampoFaltante_Devuelve400SinLlamarAlServicio()
    {
        var resultado = await CrearController().Registro(new RegistroRequest
        {
            Password = "clave123",
            Nombre = "",
            Email = "correo@ejemplo.com"
        });

        Assert.IsType<BadRequestObjectResult>(resultado);
        _authService.Verify(
            s => s.RegistrarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    // ============================================================
    // LoginMicrosoft
    // ============================================================

    [Fact]
    public async Task LoginMicrosoft_ConTokenValido_DevuelveOkConElTokenDeCasagres()
    {
        _authService.Setup(s => s.LoginConMicrosoftAsync("id-token")).ReturnsAsync("token-casagres");

        var resultado = Assert.IsType<OkObjectResult>(
            await CrearController().LoginMicrosoft(new MicrosoftLoginRequest { IdToken = "id-token" }));

        Assert.NotNull(resultado.Value);
    }

    [Fact]
    public async Task LoginMicrosoft_SinIdToken_Devuelve400()
    {
        var resultado = await CrearController().LoginMicrosoft(new MicrosoftLoginRequest { IdToken = "" });

        Assert.IsType<BadRequestObjectResult>(resultado);
    }

    [Fact]
    public async Task LoginMicrosoft_CuandoElServicioDevuelveNull_Devuelve401()
    {
        _authService.Setup(s => s.LoginConMicrosoftAsync(It.IsAny<string>())).ReturnsAsync((string?)null);

        var resultado = await CrearController().LoginMicrosoft(
            new MicrosoftLoginRequest { IdToken = "id-token-invalido" });

        Assert.IsType<UnauthorizedObjectResult>(resultado);
    }

    [Fact]
    public async Task LoginMicrosoft_CuandoElServicioLanzaExcepcion_Devuelve401()
    {
        _authService.Setup(s => s.LoginConMicrosoftAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("token corrupto"));

        var resultado = await CrearController().LoginMicrosoft(
            new MicrosoftLoginRequest { IdToken = "id-token" });

        Assert.IsType<UnauthorizedObjectResult>(resultado);
    }

    // ============================================================
    // LoginGoogle
    // ============================================================

    [Fact]
    public async Task LoginGoogle_ConTokenValido_DevuelveOkConElTokenDeCasagres()
    {
        _authService.Setup(s => s.LoginConGoogleAsync("id-token")).ReturnsAsync("token-casagres");

        var resultado = Assert.IsType<OkObjectResult>(
            await CrearController().LoginGoogle(new GoogleLoginRequest { AccessToken = "id-token" }));

        Assert.NotNull(resultado.Value);
    }

    [Fact]
    public async Task LoginGoogle_SinIdToken_Devuelve400()
    {
        var resultado = await CrearController().LoginGoogle(new GoogleLoginRequest { AccessToken = "" });

        Assert.IsType<BadRequestObjectResult>(resultado);
    }

    [Fact]
    public async Task LoginGoogle_CuandoElServicioDevuelveNull_Devuelve401()
    {
        _authService.Setup(s => s.LoginConGoogleAsync(It.IsAny<string>())).ReturnsAsync((string?)null);

        var resultado = await CrearController().LoginGoogle(
            new GoogleLoginRequest { AccessToken = "id-token-invalido" });

        Assert.IsType<UnauthorizedObjectResult>(resultado);
    }

    [Fact]
    public async Task LoginGoogle_CuandoElServicioLanzaExcepcion_Devuelve401()
    {
        _authService.Setup(s => s.LoginConGoogleAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("token corrupto"));

        var resultado = await CrearController().LoginGoogle(
            new GoogleLoginRequest { AccessToken = "id-token" });

        Assert.IsType<UnauthorizedObjectResult>(resultado);
    }

    // ============================================================
    // SolicitarReset
    // ============================================================

    [Fact]
    public async Task SolicitarReset_ConEmailValido_DevuelveOkYLlamaAlServicio()
    {
        var resultado = await CrearController().SolicitarReset(
            new SolicitarResetPasswordRequest { Email = "correo@ejemplo.com" });

        Assert.IsType<OkObjectResult>(resultado);
        _passwordResetService.Verify(
            s => s.SolicitarResetAsync("correo@ejemplo.com"), Times.Once);
    }

    [Fact]
    public async Task SolicitarReset_SinEmail_Devuelve400SinLlamarAlServicio()
    {
        var resultado = await CrearController().SolicitarReset(
            new SolicitarResetPasswordRequest { Email = "" });

        Assert.IsType<BadRequestObjectResult>(resultado);
        _passwordResetService.Verify(
            s => s.SolicitarResetAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SolicitarReset_ConEmailInexistente_DevuelveOkIgualQueConUnoExistente()
    {
        // El controlador siempre responde OK, exista o no el correo,
        // para no revelar qué correos están registrados.
        var resultado = await CrearController().SolicitarReset(
            new SolicitarResetPasswordRequest { Email = "no-existe@ejemplo.com" });

        Assert.IsType<OkObjectResult>(resultado);
    }

    // ============================================================
    // RestablecerPassword
    // ============================================================

    [Fact]
    public async Task RestablecerPassword_ConTokenValido_DevuelveOk()
    {
        _passwordResetService
            .Setup(s => s.RestablecerAsync("token-valido", "clave-nueva"))
            .ReturnsAsync(true);

        var resultado = await CrearController().RestablecerPassword(
            new RestablecerPasswordRequest { Token = "token-valido", NuevaPassword = "clave-nueva" });

        Assert.IsType<OkObjectResult>(resultado);
    }

    [Fact]
    public async Task RestablecerPassword_ConTokenInvalidoOExpirado_Devuelve400()
    {
        _passwordResetService
            .Setup(s => s.RestablecerAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var resultado = await CrearController().RestablecerPassword(
            new RestablecerPasswordRequest { Token = "token-invalido", NuevaPassword = "clave-nueva" });

        Assert.IsType<BadRequestObjectResult>(resultado);
    }

    [Theory]
    [InlineData("", "clave-nueva")]
    [InlineData("token-valido", "")]
    public async Task RestablecerPassword_ConCamposFaltantes_Devuelve400SinLlamarAlServicio(
        string token, string nuevaPassword)
    {
        var resultado = await CrearController().RestablecerPassword(
            new RestablecerPasswordRequest { Token = token, NuevaPassword = nuevaPassword });

        Assert.IsType<BadRequestObjectResult>(resultado);
        _passwordResetService.Verify(
            s => s.RestablecerAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RestablecerPassword_ConContraseñaMuyCorta_Devuelve400SinLlamarAlServicio()
    {
        var resultado = await CrearController().RestablecerPassword(
            new RestablecerPasswordRequest { Token = "token-valido", NuevaPassword = "123" });

        Assert.IsType<BadRequestObjectResult>(resultado);
        _passwordResetService.Verify(
            s => s.RestablecerAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    // ============================================================
    // VerificarEmail
    // ============================================================

    [Fact]
    public async Task VerificarEmail_ConTokenValido_DevuelveOk()
    {
        _emailVerificationService.Setup(s => s.VerificarAsync("token-valido")).ReturnsAsync(true);

        var resultado = await CrearController().VerificarEmail(
            new VerificarEmailRequest { Token = "token-valido" });

        Assert.IsType<OkObjectResult>(resultado);
    }

    [Fact]
    public async Task VerificarEmail_ConTokenInvalidoOExpirado_Devuelve400()
    {
        _emailVerificationService
            .Setup(s => s.VerificarAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        var resultado = await CrearController().VerificarEmail(
            new VerificarEmailRequest { Token = "token-invalido" });

        Assert.IsType<BadRequestObjectResult>(resultado);
    }

    [Fact]
    public async Task VerificarEmail_SinToken_Devuelve400SinLlamarAlServicio()
    {
        var resultado = await CrearController().VerificarEmail(
            new VerificarEmailRequest { Token = "" });

        Assert.IsType<BadRequestObjectResult>(resultado);
        _emailVerificationService.Verify(
            s => s.VerificarAsync(It.IsAny<string>()), Times.Never);
    }

    // ============================================================
    // ReenviarVerificacion
    // ============================================================

    [Fact]
    public async Task ReenviarVerificacion_ConEmailValido_DevuelveOkYLlamaAlServicio()
    {
        var resultado = await CrearController().ReenviarVerificacion(
            new ReenviarVerificacionRequest { Email = "correo@ejemplo.com" });

        Assert.IsType<OkObjectResult>(resultado);
        _emailVerificationService.Verify(
            s => s.ReenviarSiNoVerificadoAsync("correo@ejemplo.com"), Times.Once);
    }

    [Fact]
    public async Task ReenviarVerificacion_SinEmail_Devuelve400SinLlamarAlServicio()
    {
        var resultado = await CrearController().ReenviarVerificacion(
            new ReenviarVerificacionRequest { Email = "" });

        Assert.IsType<BadRequestObjectResult>(resultado);
        _emailVerificationService.Verify(
            s => s.ReenviarSiNoVerificadoAsync(It.IsAny<string>()), Times.Never);
    }

    // ============================================================
    // SubirFotoPerfil
    // ============================================================

    private static IFormFile CrearArchivoDePrueba(
        string contentType = "image/jpeg", int tamanoBytes = 10)
    {
        var stream = new MemoryStream(new byte[tamanoBytes]);

        return new FormFile(stream, 0, stream.Length, "foto", "foto.jpg")
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
    }

    [Fact]
    public async Task SubirFotoPerfil_ConImagenValida_DevuelveOkConLaUrl()
    {
        _fotoPerfilService
            .Setup(s => s.GuardarFotoAsync(1, It.IsAny<Stream>(), It.IsAny<long>(), "image/jpeg"))
            .ReturnsAsync("/api/auth/foto-perfil/1?v=123");

        var resultado = Assert.IsType<OkObjectResult>(
            await CrearControllerAutenticado(1).SubirFotoPerfil(CrearArchivoDePrueba()));

        Assert.NotNull(resultado.Value);
    }

    [Fact]
    public async Task SubirFotoPerfil_SinArchivo_Devuelve400SinLlamarAlServicio()
    {
        var resultado = await CrearControllerAutenticado(1).SubirFotoPerfil(null);

        Assert.IsType<BadRequestObjectResult>(resultado);
        _fotoPerfilService.Verify(
            s => s.GuardarFotoAsync(
                It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<long>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task SubirFotoPerfil_SinClaimDeIdentificador_Devuelve401()
    {
        var controller = CrearController();

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            }
        };

        var resultado = await controller.SubirFotoPerfil(CrearArchivoDePrueba());

        Assert.IsType<UnauthorizedResult>(resultado);
    }

    [Fact]
    public async Task SubirFotoPerfil_CuandoElServicioRechazaElFormato_Devuelve400ConElMensaje()
    {
        _fotoPerfilService
            .Setup(s => s.GuardarFotoAsync(
                It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<long>(), It.IsAny<string>()))
            .ThrowsAsync(new ArgumentException("Formato de imagen no soportado. Usa JPG, PNG o WEBP."));

        var resultado = Assert.IsType<BadRequestObjectResult>(
            await CrearControllerAutenticado(1).SubirFotoPerfil(CrearArchivoDePrueba("image/gif")));

        Assert.NotNull(resultado.Value);
    }

    // ============================================================
    // ObtenerFotoPerfil
    // ============================================================

    [Fact]
    public void ObtenerFotoPerfil_ConFotoExistente_DevuelveElArchivo()
    {
        using var stream = new MemoryStream();
        _fotoPerfilService
            .Setup(s => s.ObtenerFoto(1))
            .Returns((stream, "image/png"));

        var resultado = Assert.IsType<FileStreamResult>(CrearController().ObtenerFotoPerfil(1));

        Assert.Equal("image/png", resultado.ContentType);
    }

    [Fact]
    public void ObtenerFotoPerfil_SinFoto_Devuelve404()
    {
        _fotoPerfilService.Setup(s => s.ObtenerFoto(1)).Returns((ValueTuple<Stream, string>?)null);

        Assert.IsType<NotFoundResult>(CrearController().ObtenerFotoPerfil(1));
    }
}
