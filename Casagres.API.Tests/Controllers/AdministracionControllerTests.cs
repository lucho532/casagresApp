using System.Security.Claims;
using Casagres.API.Controllers;
using Casagres.API.Data;
using Casagres.API.Models;
using Casagres.API.Models.Dtos.Administracion;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Casagres.API.Tests.Controllers;

public class AdministracionControllerTests
{
    private static CasagresDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<CasagresDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CasagresDbContext(opciones);
    }

    private static AdministracionController CrearController(CasagresDbContext db, long usuarioActualId)
    {
        var controller = new AdministracionController(db);

        var identidad = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, usuarioActualId.ToString()) },
            "TestAuth");

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identidad) }
        };

        return controller;
    }

    [Fact]
    public async Task ObtenerUsuarios_DevuelveLaListaDeUsuarios()
    {
        await using var db = CrearContexto();
        db.Usuarios.Add(new Usuario { Id = 2, UsuarioNombre = "b" });
        db.Usuarios.Add(new Usuario { Id = 1, UsuarioNombre = "a" });
        await db.SaveChangesAsync();

        var resultado = Assert.IsType<OkObjectResult>(
            await CrearController(db, usuarioActualId: 1).ObtenerUsuarios());

        Assert.NotNull(resultado.Value);
    }

    // ============================================================
    // CambiarRol
    // ============================================================

    [Fact]
    public async Task CambiarRol_SinRolEnElBody_Devuelve400()
    {
        await using var db = CrearContexto();

        var resultado = await CrearController(db, usuarioActualId: 1)
            .CambiarRol(1, new CambiarRolRequest { Rol = "" });

        Assert.IsType<BadRequestObjectResult>(resultado);
    }

    [Fact]
    public async Task CambiarRol_ConUnRolQueNoExiste_Devuelve400()
    {
        await using var db = CrearContexto();
        db.Usuarios.Add(new Usuario { Id = 1, UsuarioNombre = "a", Rol = "usuario" });
        await db.SaveChangesAsync();

        var resultado = await CrearController(db, usuarioActualId: 2)
            .CambiarRol(1, new CambiarRolRequest { Rol = "superadmin" });

        Assert.IsType<BadRequestObjectResult>(resultado);
    }

    [Fact]
    public async Task CambiarRol_ConUsuarioInexistente_Devuelve404()
    {
        await using var db = CrearContexto();

        var resultado = await CrearController(db, usuarioActualId: 1)
            .CambiarRol(999, new CambiarRolRequest { Rol = "admin" });

        Assert.IsType<NotFoundObjectResult>(resultado);
    }

    [Fact]
    public async Task CambiarRol_UnAdminNoPuedeQuitarseSuPropioRolDeAdmin()
    {
        await using var db = CrearContexto();
        db.Usuarios.Add(new Usuario { Id = 1, UsuarioNombre = "admin1", Rol = "admin" });
        await db.SaveChangesAsync();

        var resultado = await CrearController(db, usuarioActualId: 1)
            .CambiarRol(1, new CambiarRolRequest { Rol = "usuario" });

        Assert.IsType<BadRequestObjectResult>(resultado);

        var usuario = await db.Usuarios.SingleAsync(u => u.Id == 1);
        Assert.Equal("admin", usuario.Rol);
    }

    [Fact]
    public async Task CambiarRol_ConDatosValidos_ActualizaElRolDelUsuario()
    {
        await using var db = CrearContexto();
        db.Usuarios.Add(new Usuario { Id = 1, UsuarioNombre = "usuario1", Rol = "usuario" });
        await db.SaveChangesAsync();

        var resultado = Assert.IsType<OkObjectResult>(
            await CrearController(db, usuarioActualId: 2)
                .CambiarRol(1, new CambiarRolRequest { Rol = "admin" }));

        Assert.NotNull(resultado.Value);

        var usuario = await db.Usuarios.SingleAsync(u => u.Id == 1);
        Assert.Equal("admin", usuario.Rol);
    }

    // ============================================================
    // CambiarEstado
    // ============================================================

    [Fact]
    public async Task CambiarEstado_ConUsuarioInexistente_Devuelve404()
    {
        await using var db = CrearContexto();

        var resultado = await CrearController(db, usuarioActualId: 1)
            .CambiarEstado(999, new CambiarEstadoRequest { Activo = false });

        Assert.IsType<NotFoundObjectResult>(resultado);
    }

    [Fact]
    public async Task CambiarEstado_UnAdminNoPuedeDesactivarseASiMismo()
    {
        await using var db = CrearContexto();
        db.Usuarios.Add(new Usuario { Id = 1, UsuarioNombre = "admin1", Activo = true });
        await db.SaveChangesAsync();

        var resultado = await CrearController(db, usuarioActualId: 1)
            .CambiarEstado(1, new CambiarEstadoRequest { Activo = false });

        Assert.IsType<BadRequestObjectResult>(resultado);

        var usuario = await db.Usuarios.SingleAsync(u => u.Id == 1);
        Assert.True(usuario.Activo);
    }

    [Fact]
    public async Task CambiarEstado_ConDatosValidos_ActualizaElEstadoDelUsuario()
    {
        await using var db = CrearContexto();
        db.Usuarios.Add(new Usuario { Id = 1, UsuarioNombre = "usuario1", Activo = true });
        await db.SaveChangesAsync();

        var resultado = Assert.IsType<OkObjectResult>(
            await CrearController(db, usuarioActualId: 2)
                .CambiarEstado(1, new CambiarEstadoRequest { Activo = false }));

        Assert.NotNull(resultado.Value);

        var usuario = await db.Usuarios.SingleAsync(u => u.Id == 1);
        Assert.False(usuario.Activo);
    }

    // ============================================================
    // EliminarUsuario
    // ============================================================

    [Fact]
    public async Task EliminarUsuario_ConUsuarioInexistente_Devuelve404()
    {
        await using var db = CrearContexto();

        var resultado = await CrearController(db, usuarioActualId: 1)
            .EliminarUsuario(999);

        Assert.IsType<NotFoundObjectResult>(resultado);
    }

    [Fact]
    public async Task EliminarUsuario_UnAdminNoPuedeEliminarseASiMismo()
    {
        await using var db = CrearContexto();
        db.Usuarios.Add(new Usuario { Id = 1, UsuarioNombre = "admin1" });
        await db.SaveChangesAsync();

        var resultado = await CrearController(db, usuarioActualId: 1)
            .EliminarUsuario(1);

        Assert.IsType<BadRequestObjectResult>(resultado);
        Assert.True(await db.Usuarios.AnyAsync(u => u.Id == 1));
    }

    [Fact]
    public async Task EliminarUsuario_ConUsuarioValido_LoEliminaDeLaBaseDeDatos()
    {
        await using var db = CrearContexto();
        db.Usuarios.Add(new Usuario { Id = 1, UsuarioNombre = "usuario1" });
        await db.SaveChangesAsync();

        var resultado = Assert.IsType<OkObjectResult>(
            await CrearController(db, usuarioActualId: 2).EliminarUsuario(1));

        Assert.NotNull(resultado.Value);
        Assert.False(await db.Usuarios.AnyAsync(u => u.Id == 1));
    }

    [Fact]
    public async Task EliminarUsuario_ConTokensAsociados_LosEliminaJuntoConElUsuario()
    {
        await using var db = CrearContexto();
        db.Usuarios.Add(new Usuario { Id = 1, UsuarioNombre = "usuario1" });
        await db.SaveChangesAsync();

        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UsuarioId = 1,
            Token = "token-reset",
            FechaExpiracion = DateTime.UtcNow.AddHours(1),
        });
        db.EmailVerificationTokens.Add(new EmailVerificationToken
        {
            UsuarioId = 1,
            Token = "token-verificacion",
            FechaExpiracion = DateTime.UtcNow.AddHours(1),
        });
        await db.SaveChangesAsync();

        var resultado = await CrearController(db, usuarioActualId: 2).EliminarUsuario(1);

        Assert.IsType<OkObjectResult>(resultado);
        Assert.False(await db.Usuarios.AnyAsync(u => u.Id == 1));
        Assert.Empty(db.PasswordResetTokens);
        Assert.Empty(db.EmailVerificationTokens);
    }
}
