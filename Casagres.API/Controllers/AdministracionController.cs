using Casagres.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Casagres.API.Data;

namespace Casagres.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "admin")]
public class AdministracionController : ControllerBase
{
    private readonly CasagresDbContext _db;

    public AdministracionController(CasagresDbContext db)
    {
        _db = db;
    }

    [HttpGet("usuarios")]
    public async Task<IActionResult> ObtenerUsuarios()
    {
        var usuarios = await _db.Usuarios
            .OrderBy(u => u.Id)
            .Select(u => new
            {
                u.Id,
                Usuario = u.UsuarioNombre,
                u.Nombre,
                u.Email,
                u.Rol,
                u.Activo,
                u.FechaCreacion
            })
            .ToListAsync();

        return Ok(usuarios);
    }

    [HttpPut("usuarios/{id}/rol")]
    public async Task<IActionResult> CambiarRol(
    long id,
    [FromBody] CambiarRolRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Rol))
        {
            return BadRequest(new
            {
                mensaje = "El rol es obligatorio."
            });
        }

        var rol = request.Rol.Trim().ToLower();

        if (rol != "admin" && rol != "usuario")
        {
            return BadRequest(new
            {
                mensaje = "El rol debe ser 'admin' o 'usuario'."
            });
        }

        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Id == id);

        if (usuario == null)
        {
            return NotFound(new
            {
                mensaje = "El usuario no existe."
            });
        }

        var usuarioActualId = User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier
        )?.Value;

        if (usuarioActualId == id.ToString() && rol != "admin")
        {
            return BadRequest(new
            {
                mensaje = "No puedes quitarte el rol de administrador a ti mismo."
            });
        }

        usuario.Rol = rol;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Rol actualizado correctamente.",
            usuario.Id,
            usuario.Rol
        });
    }

    public class CambiarRolRequest
    {
        public string Rol { get; set; } = string.Empty;
    }


    [HttpPut("usuarios/{id}/estado")]
    public async Task<IActionResult> CambiarEstado(
    long id,
    [FromBody] CambiarEstadoRequest request)
    {
        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Id == id);

        if (usuario == null)
        {
            return NotFound(new
            {
                mensaje = "El usuario no existe."
            });
        }

        var usuarioActualId = User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier
        )?.Value;

        // Un administrador no puede desactivar su propio usuario
        if (usuarioActualId == id.ToString() && request.Activo == false)
        {
            return BadRequest(new
            {
                mensaje = "No puedes desactivar tu propio usuario."
            });
        }

        usuario.Activo = request.Activo;

        await _db.SaveChangesAsync();

        return Ok(new
        {
            mensaje = request.Activo
                ? "Usuario activado correctamente."
                : "Usuario desactivado correctamente.",

            usuario.Id,
            usuario.Activo
        });
    }

    public class CambiarEstadoRequest
    {
        public bool Activo { get; set; }
    }
}