using Casagres.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;


namespace Casagres.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }


    [AllowAnonymous]
    [HttpGet]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
            [FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Usuario) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new
            {
                mensaje = "Usuario y contraseña son obligatorios."
            });
        }

        var token = await _authService.LoginAsync(
            request.Usuario,
            request.Password);

        if (token == null)
        {
            return Unauthorized(new
            {
                mensaje = "Usuario o contraseña incorrectos."
            });
        }

        return Ok(new
        {
            token
        });
    }


    [AllowAnonymous]
    [HttpPost("registro")]
    public async Task<IActionResult> Registro(
        [FromBody] RegistroRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Usuario) ||
            string.IsNullOrWhiteSpace(request.Password) ||
            string.IsNullOrWhiteSpace(request.Nombre) ||
            string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new
            {
                mensaje = "Todos los campos son obligatorios."
            });
        }

        var registrado = await _authService.RegistrarAsync(
            request.Usuario,
            request.Password,
            request.Nombre,
            request.Email);

        if (!registrado)
        {
            return Conflict(new
            {
                mensaje = "El usuario o correo electrónico ya existe."
            });
        }

        return Ok(new
        {
            mensaje = "Usuario creado correctamente."
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
}





public class MicrosoftLoginRequest
{
    public string IdToken { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Usuario { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

public class RegistroRequest
{
    public string Usuario { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}