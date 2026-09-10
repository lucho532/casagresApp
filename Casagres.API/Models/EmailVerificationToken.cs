namespace Casagres.API.Models;

public class EmailVerificationToken
{
    public long Id { get; set; }

    public long UsuarioId { get; set; }

    public string Token { get; set; } = string.Empty;

    public DateTime FechaExpiracion { get; set; }

    public bool Usado { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
