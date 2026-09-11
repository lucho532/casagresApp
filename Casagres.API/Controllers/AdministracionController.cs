using Casagres.API.Models;
using Casagres.API.Models.Dtos.Administracion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Casagres.API.Data;

namespace Casagres.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.Admin)]
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

        if (rol != Roles.Admin && rol != Roles.Usuario)
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

    [HttpDelete("usuarios/{id}")]
    public async Task<IActionResult> EliminarUsuario(long id)
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

        if (usuarioActualId == id.ToString())
        {
            return BadRequest(new
            {
                mensaje = "No puedes eliminar tu propio usuario."
            });
        }

        // Los tokens de reset/verificación tienen borrado en cascada a
        // nivel de base de datos (ON DELETE CASCADE): Postgres los
        // elimina automáticamente al borrar el usuario.
        _db.Usuarios.Remove(usuario);

        await _db.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Usuario eliminado correctamente."
        });
    }
}