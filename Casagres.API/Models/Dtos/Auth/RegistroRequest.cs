namespace Casagres.API.Models.Dtos.Auth;

public class RegistroRequest
{
    public string Usuario { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}
