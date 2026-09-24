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
    public string Rol { get; set; } = Roles.Pendiente;

    public bool EmailVerificado { get; set; }

    // Ruta relativa servida por el propio backend (/api/auth/foto-perfil/{id})
    // hacia la foto que el usuario subió manualmente. Nula si nunca subió
    // ninguna (el frontend muestra sus iniciales en ese caso).
    public string? FotoUrl { get; set; }
}