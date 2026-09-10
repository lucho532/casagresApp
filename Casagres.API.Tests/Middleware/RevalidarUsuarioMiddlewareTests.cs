using System.Security.Claims;
using Casagres.API.Data;
using Casagres.API.Middleware;
using Casagres.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Casagres.API.Tests.Middleware;

public class RevalidarUsuarioMiddlewareTests
{
    private static CasagresDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<CasagresDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CasagresDbContext(opciones);
    }

    private static HttpContext CrearContextoHttp(long? usuarioId, string? rolEnToken = null)
    {
        var contexto = new DefaultHttpContext();
        contexto.Response.Body = new MemoryStream();

        if (usuarioId == null)
        {
            return contexto;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuarioId.ToString()!),
        };

        if (rolEnToken != null)
        {
            claims.Add(new Claim(ClaimTypes.Role, rolEnToken));
        }

        contexto.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        return contexto;
    }

    private static (RevalidarUsuarioMiddleware Middleware, Func<bool> SeLlamoASiguiente) CrearMiddleware()
    {
        var siguienteLlamado = false;

        RequestDelegate siguiente = _ =>
        {
            siguienteLlamado = true;
            return Task.CompletedTask;
        };

        return (new RevalidarUsuarioMiddleware(siguiente), () => siguienteLlamado);
    }

    [Fact]
    public async Task InvokeAsync_ConPeticionSinAutenticar_LlamaASiguienteSinTocarNada()
    {
        await using var db = CrearContexto();
        var contexto = CrearContextoHttp(usuarioId: null);
        var (middleware, seLlamoASiguiente) = CrearMiddleware();

        await middleware.InvokeAsync(contexto, db);

        Assert.True(seLlamoASiguiente());
        Assert.Equal(200, contexto.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ConUsuarioActivo_LlamaASiguiente()
    {
        await using var db = CrearContexto();

        db.Usuarios.Add(new Usuario { Id = 1, UsuarioNombre = "jperez", Activo = true, Rol = Roles.Usuario });
        await db.SaveChangesAsync();

        var contexto = CrearContextoHttp(usuarioId: 1, rolEnToken: Roles.Usuario);
        var (middleware, seLlamoASiguiente) = CrearMiddleware();

        await middleware.InvokeAsync(contexto, db);

        Assert.True(seLlamoASiguiente());
    }

    [Fact]
    public async Task InvokeAsync_ConUsuarioDesactivado_Devuelve401YNoLlamaASiguiente()
    {
        await using var db = CrearContexto();

        db.Usuarios.Add(new Usuario { Id = 1, UsuarioNombre = "jperez", Activo = false, Rol = Roles.Usuario });
        await db.SaveChangesAsync();

        var contexto = CrearContextoHttp(usuarioId: 1, rolEnToken: Roles.Usuario);
        var (middleware, seLlamoASiguiente) = CrearMiddleware();

        await middleware.InvokeAsync(contexto, db);

        Assert.False(seLlamoASiguiente());
        Assert.Equal(StatusCodes.Status401Unauthorized, contexto.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ConUsuarioQueYaNoExiste_Devuelve401YNoLlamaASiguiente()
    {
        await using var db = CrearContexto();
        var contexto = CrearContextoHttp(usuarioId: 999, rolEnToken: Roles.Usuario);
        var (middleware, seLlamoASiguiente) = CrearMiddleware();

        await middleware.InvokeAsync(contexto, db);

        Assert.False(seLlamoASiguiente());
        Assert.Equal(StatusCodes.Status401Unauthorized, contexto.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ConRolCambiadoEnLaBaseDeDatos_ActualizaElClaimDeRolAntesDeContinuar()
    {
        await using var db = CrearContexto();

        // El token viejo dice "usuario", pero un admin ya lo ascendió.
        db.Usuarios.Add(new Usuario { Id = 1, UsuarioNombre = "jperez", Activo = true, Rol = Roles.Admin });
        await db.SaveChangesAsync();

        var contexto = CrearContextoHttp(usuarioId: 1, rolEnToken: Roles.Usuario);
        var (middleware, seLlamoASiguiente) = CrearMiddleware();

        await middleware.InvokeAsync(contexto, db);

        Assert.True(seLlamoASiguiente());
        Assert.True(contexto.User.IsInRole(Roles.Admin));
        Assert.False(contexto.User.IsInRole(Roles.Usuario));
    }

    [Fact]
    public async Task InvokeAsync_ConUsuarioAprobadoDePendienteAUsuario_ActualizaElClaimDeInmediato()
    {
        await using var db = CrearContexto();

        db.Usuarios.Add(new Usuario { Id = 1, UsuarioNombre = "jperez", Activo = true, Rol = Roles.Usuario });
        await db.SaveChangesAsync();

        // El token todavía trae el rol "pendiente" con el que inició sesión.
        var contexto = CrearContextoHttp(usuarioId: 1, rolEnToken: Roles.Pendiente);
        var (middleware, seLlamoASiguiente) = CrearMiddleware();

        await middleware.InvokeAsync(contexto, db);

        Assert.True(seLlamoASiguiente());
        Assert.True(contexto.User.IsInRole(Roles.Usuario));
        Assert.False(contexto.User.IsInRole(Roles.Pendiente));
    }
}
