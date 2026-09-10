using System.Security.Claims;
using Casagres.API.Data;
using Microsoft.EntityFrameworkCore;

namespace Casagres.API.Middleware;

// El JWT queda "congelado" con el rol y sin ningún chequeo de "Activo"
// desde el momento del login, y es válido hasta por 2 horas. Sin este
// middleware, desactivar a un usuario o cambiarle el rol no tendría
// ningún efecto hasta que su token expirara.
//
// En cada petición autenticada, este middleware vuelve a consultar el
// estado actual del usuario en la base de datos:
// - Si ya no existe o está desactivado, corta la petición con 401.
// - Si su rol cambió, reemplaza el claim de rol del token por el rol
//   vigente, para que los [Authorize(Roles = "...")] de los
//   controladores evalúen siempre el rol actual, no el de hace rato.
public class RevalidarUsuarioMiddleware
{
    private readonly RequestDelegate _next;

    public RevalidarUsuarioMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, CasagresDbContext db)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var puedeContinuar = await RevalidarAsync(context, db);

            if (!puedeContinuar)
            {
                return;
            }
        }

        await _next(context);
    }

    private static async Task<bool> RevalidarAsync(HttpContext context, CasagresDbContext db)
    {
        var idClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!long.TryParse(idClaim, out var usuarioId))
        {
            return true;
        }

        var usuario = await db.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == usuarioId);

        if (usuario == null || !usuario.Activo)
        {
            await RechazarAsync(context);
            return false;
        }

        ActualizarClaimDeRol(context, usuario.Rol);

        return true;
    }

    private static async Task RechazarAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new
        {
            mensaje = "La sesión ya no es válida."
        });
    }

    private static void ActualizarClaimDeRol(HttpContext context, string rolActual)
    {
        if (context.User.Identity is not ClaimsIdentity identidad)
        {
            return;
        }

        var claimAnterior = identidad.FindFirst(ClaimTypes.Role);

        if (claimAnterior != null)
        {
            identidad.RemoveClaim(claimAnterior);
        }

        identidad.AddClaim(new Claim(ClaimTypes.Role, rolActual));
    }
}
