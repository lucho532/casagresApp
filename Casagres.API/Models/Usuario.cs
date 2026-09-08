namespace Casagres.API.Models;

public class Usuario
{
    public long Id { get; set; }

    public string UsuarioNombre { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string? Nombre { get; set; }

    public string? Email { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // NUEVO: Rol del usuario
    public string Rol { get; set; } = "usuario";
}