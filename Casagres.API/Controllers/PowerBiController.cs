using Casagres.API.Data;
using Casagres.API.Models;
using Casagres.API.Models.Dtos.PowerBi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Casagres.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Roles.ConAccesoADatos)]
public class PowerBiController : ControllerBase
{
    private readonly CasagresDbContext _db;

    public PowerBiController(CasagresDbContext db)
    {
        _db = db;
    }

    // =========================================
    // LISTAR TABLEROS (el más reciente primero)
    // =========================================

    [HttpGet]
    public async Task<IActionResult> ObtenerTableros()
    {
        var tableros = await _db.PowerBiTableros
            .OrderByDescending(t => t.FechaCreacion)
            .ToListAsync();

        return Ok(tableros);
    }

    // =========================================
    // AGREGAR TABLERO (solo administradores)
    // =========================================

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> AgregarTablero(
        [FromBody] AgregarTableroRequest request)
    {
        if (request == null
            || string.IsNullOrWhiteSpace(request.Nombre)
            || string.IsNullOrWhiteSpace(request.Url))
        {
            return BadRequest(new
            {
                mensaje = "El nombre y la URL son obligatorios."
            });
        }

        var urlLimpia = request.Url.Trim();

        var urlValida = Uri.TryCreate(urlLimpia, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

        if (!urlValida)
        {
            return BadRequest(new
            {
                mensaje = "La URL no es válida."
            });
        }

        var tablero = new PowerBiTablero
        {
            Nombre = request.Nombre.Trim(),
            Url = urlLimpia,
            FechaCreacion = DateTime.UtcNow,
        };

        _db.PowerBiTableros.Add(tablero);

        await _db.SaveChangesAsync();

        return Ok(tablero);
    }

    // =========================================
    // ELIMINAR TABLERO (solo administradores)
    // =========================================

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> EliminarTablero(long id)
    {
        var tablero = await _db.PowerBiTableros
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tablero == null)
        {
            return NotFound(new
            {
                mensaje = "El tablero no existe."
            });
        }

        _db.PowerBiTableros.Remove(tablero);

        await _db.SaveChangesAsync();

        return Ok(new
        {
            mensaje = "Tablero eliminado correctamente."
        });
    }
}
