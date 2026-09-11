using Casagres.API.Models.Dtos.Auth;
using Casagres.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;


namespace Casagres.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IPasswordResetService _passwordResetService;
    private readonly IEmailVerificationService _emailVerificationService;

    public AuthController(
        IAuthService authService,
        IPasswordResetService passwordResetService,
        IEmailVerificationService emailVerificationService)
    {
        _authService = authService;
        _passwordResetService = passwordResetService;
        _emailVerificationService = emailVerificationService;
    }


    // ============================================================
    // PERFIL
    // ============================================================
    //
    // A diferencia del rol embebido en el JWT (que queda "congelado" al
    // momento del login por hasta 2 horas), este endpoint siempre
    // consulta el estado actual en la base de datos. El frontend lo usa
    // para saber si a un usuario "pendiente" ya lo aprobaron, sin
    // esperar a que expire su token.

    [HttpGet("perfil")]
    public async Task<IActionResult> Perfil()
    {
        var idClaim = User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (!long.TryParse(idClaim, out var id))
        {
            return Unauthorized();
        }

        var usuario = await _authService.ObtenerPorIdAsync(id);

        if (usuario == null)
        {
            return Unauthorized();
        }

        return Ok(new
        {
            usuario.Id,
            Usuario = usuario.UsuarioNombre,
            usuario.Nombre,
            usuario.Email,
            usuario.Rol,
            usuario.Activo
        });
    }

    [AllowAnonymous]
    [HttpGet]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
            [FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new
            {
                mensaje = "Correo electrónico y contraseña son obligatorios."
            });
        }

        try
        {
            var token = await _authService.LoginAsync(
                request.Email,
                request.Password);

            if (token == null)
            {
                return Unauthorized(new
                {
                    mensaje = "Correo electrónico o contraseña incorrectos."
                });
            }

            return Ok(new
            {
                token
            });
        }
        catch (EmailNoVerificadoException ex)
        {
            return Unauthorized(new
            {
                mensaje = "Debes verificar tu correo electrónico antes de iniciar sesión.",
                codigo = "EMAIL_NO_VERIFICADO",
                email = ex.Email
            });
        }
    }


    [AllowAnonymous]
    [HttpPost("registro")]
    public async Task<IActionResult> Registro(
        [FromBody] RegistroRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.Nombre) ||
            string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new
            {
                mensaje = "Todos los campos son obligatorios."
            });
        }

        var registrado = await _authService.RegistrarAsync(
            request.Password,
            request.Nombre,
            request.Email);

        if (!registrado)
        {
            return Conflict(new
            {
                mensaje = "Ese correo electrónico ya está registrado."
            });
        }

        return Ok(new
        {
            mensaje =
                "Usuario creado correctamente. Revisa tu correo para verificar tu cuenta antes de iniciar sesión."
        });
    }

    // ============================================================
    // LOGIN CON MICROSOFT
    // ============================================================

    [AllowAnonymous]
    [HttpPost("microsoft")]
    public async Task<IActionResult> LoginMicrosoft(
        [FromBody] MicrosoftLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            return BadRequest(new
            {
                mensaje = "El token de Microsoft es obligatorio."
            });
        }

        try
        {
            var token = await _authService
                .LoginConMicrosoftAsync(request.IdToken);

            if (token == null)
            {
                return Unauthorized(new
                {
                    mensaje =
                        "No fue posible validar la cuenta de Microsoft."
                });
            }

            return Ok(new
            {
                token
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Error en login Microsoft: {ex.Message}");

            return Unauthorized(new
            {
                mensaje =
                    "El token de Microsoft no es válido."
            });
        }
    }

    // ============================================================
    // LOGIN CON GOOGLE
    // ============================================================

    [AllowAnonymous]
    [HttpPost("google")]
    public async Task<IActionResult> LoginGoogle(
        [FromBody] GoogleLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AccessToken))
        {
            return BadRequest(new
            {
                mensaje = "El token de Google es obligatorio."
            });
        }

        try
        {
            var token = await _authService
                .LoginConGoogleAsync(request.AccessToken);

            if (token == null)
            {
                return Unauthorized(new
                {
                    mensaje =
                        "No fue posible validar la cuenta de Google."
                });
            }

            return Ok(new
            {
                token
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Error en login Google: {ex.Message}");

            return Unauthorized(new
            {
                mensaje =
                    "El token de Google no es válido."
            });
        }
    }

    // ============================================================
    // RECUPERACIÓN DE CONTRASEÑA
    // ============================================================

    [AllowAnonymous]
    [HttpPost("solicitar-reset")]
    public async Task<IActionResult> SolicitarReset(
        [FromBody] SolicitarResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new
            {
                mensaje = "El correo electrónico es obligatorio."
            });
        }

        await _passwordResetService.SolicitarResetAsync(request.Email);

        // Respuesta genérica siempre: no revela si el correo existe.
        return Ok(new
        {
            mensaje =
                "Si el correo está registrado, recibirás un enlace para restablecer tu contraseña."
        });
    }

    [AllowAnonymous]
    [HttpPost("restablecer-password")]
    public async Task<IActionResult> RestablecerPassword(
        [FromBody] RestablecerPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token) ||
            string.IsNullOrWhiteSpace(request.NuevaPassword))
        {
            return BadRequest(new
            {
                mensaje = "El token y la nueva contraseña son obligatorios."
            });
        }

        if (request.NuevaPassword.Length < 6)
        {
            return BadRequest(new
            {
                mensaje = "La contraseña debe tener al menos 6 caracteres."
            });
        }

        var exito = await _passwordResetService.RestablecerAsync(
            request.Token,
            request.NuevaPassword);

        if (!exito)
        {
            return BadRequest(new
            {
                mensaje = "El enlace no es válido o ya expiró."
            });
        }

        return Ok(new
        {
            mensaje = "Contraseña actualizada correctamente."
        });
    }

    // ============================================================
    // VERIFICACIÓN DE CORREO
    // ============================================================

    [AllowAnonymous]
    [HttpPost("verificar-email")]
    public async Task<IActionResult> VerificarEmail(
        [FromBody] VerificarEmailRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return BadRequest(new
            {
                mensaje = "El token es obligatorio."
            });
        }

        var exito = await _emailVerificationService.VerificarAsync(request.Token);

        if (!exito)
        {
            return BadRequest(new
            {
                mensaje = "El enlace de verificación no es válido o ya expiró."
            });
        }

        return Ok(new
        {
            mensaje = "Correo verificado correctamente. Ya puedes iniciar sesión."
        });
    }

    [AllowAnonymous]
    [HttpPost("reenviar-verificacion")]
    public async Task<IActionResult> ReenviarVerificacion(
        [FromBody] ReenviarVerificacionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new
            {
                mensaje = "El correo electrónico es obligatorio."
            });
        }

        await _emailVerificationService.ReenviarSiNoVerificadoAsync(request.Email);

        // Respuesta genérica siempre: no revela si el correo existe o ya
        // está verificado.
        return Ok(new
        {
            mensaje =
                "Si el correo está registrado y pendiente de verificar, recibirás un nuevo enlace."
        });
    }
}