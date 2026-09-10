namespace Casagres.API.Models.Dtos.Auth;

public class RestablecerPasswordRequest
{
    public string Token { get; set; } = string.Empty;

    public string NuevaPassword { get; set; } = string.Empty;
}
