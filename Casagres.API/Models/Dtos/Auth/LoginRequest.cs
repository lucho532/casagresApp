namespace Casagres.API.Models.Dtos.Auth;

public class LoginRequest
{
    public string Usuario { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
