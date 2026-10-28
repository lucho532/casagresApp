using Casagres.API.Controllers;
using Casagres.API.Data;
using Casagres.API.Models;
using Casagres.API.Models.Dtos.PowerBi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Casagres.API.Tests.Controllers;

public class PowerBiControllerTests
{
    private static CasagresDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<CasagresDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CasagresDbContext(opciones);
    }

    // ============================================================
    // ObtenerTableros
    // ============================================================

    [Fact]
    public async Task ObtenerTableros_DevuelveLosTablerosOrdenadosDelMasRecienteAlMasAntiguo()
    {
        await using var db = CrearContexto();

        db.PowerBiTableros.Add(new PowerBiTablero
        {
            Id = 1,
            Nombre = "Primero",
            Url = "https://app.powerbi.com/view?r=uno",
            FechaCreacion = new DateTime(2025, 1, 1),
        });

        db.PowerBiTableros.Add(new PowerBiTablero
        {
            Id = 2,
            Nombre = "Segundo",
            Url = "https://app.powerbi.com/view?r=dos",
            FechaCreacion = new DateTime(2025, 6, 1),
        });

        await db.SaveChangesAsync();

        var resultado = Assert.IsType<OkObjectResult>(
            await new PowerBiController(db).ObtenerTableros());

        var tableros = Assert.IsAssignableFrom<List<PowerBiTablero>>(resultado.Value);

        Assert.Equal(["Segundo", "Primero"], tableros.Select(t => t.Nombre));
    }

    [Fact]
    public async Task ObtenerTableros_SinTableros_DevuelveListaVacia()
    {
        await using var db = CrearContexto();

        var resultado = Assert.IsType<OkObjectResult>(
            await new PowerBiController(db).ObtenerTableros());

        var tableros = Assert.IsAssignableFrom<List<PowerBiTablero>>(resultado.Value);

        Assert.Empty(tableros);
    }

    // ============================================================
    // AgregarTablero
    // ============================================================

    [Fact]
    public async Task AgregarTablero_ConDatosValidos_LoGuardaYLoDevuelve()
    {
        await using var db = CrearContexto();

        var resultado = Assert.IsType<OkObjectResult>(
            await new PowerBiController(db).AgregarTablero(new AgregarTableroRequest
            {
                Nombre = "Ventas 2026",
                Url = "https://app.powerbi.com/view?r=abc123",
            }));

        var tablero = Assert.IsType<PowerBiTablero>(resultado.Value);

        Assert.Equal("Ventas 2026", tablero.Nombre);
        Assert.Equal("https://app.powerbi.com/view?r=abc123", tablero.Url);
        Assert.Single(db.PowerBiTableros);
    }

    [Fact]
    public async Task AgregarTablero_SinNombre_Devuelve400()
    {
        await using var db = CrearContexto();

        var resultado = await new PowerBiController(db).AgregarTablero(
            new AgregarTableroRequest { Nombre = "", Url = "https://app.powerbi.com/view?r=abc" });

        Assert.IsType<BadRequestObjectResult>(resultado);
        Assert.Empty(db.PowerBiTableros);
    }

    [Fact]
    public async Task AgregarTablero_SinUrl_Devuelve400()
    {
        await using var db = CrearContexto();

        var resultado = await new PowerBiController(db).AgregarTablero(
            new AgregarTableroRequest { Nombre = "Ventas", Url = "" });

        Assert.IsType<BadRequestObjectResult>(resultado);
    }

    [Theory]
    [InlineData("no-es-una-url")]
    [InlineData("ftp://app.powerbi.com/view?r=abc")]
    [InlineData("javascript:alert(1)")]
    public async Task AgregarTablero_ConUnaUrlInvalida_Devuelve400(string urlInvalida)
    {
        await using var db = CrearContexto();

        var resultado = await new PowerBiController(db).AgregarTablero(
            new AgregarTableroRequest { Nombre = "Ventas", Url = urlInvalida });

        Assert.IsType<BadRequestObjectResult>(resultado);
        Assert.Empty(db.PowerBiTableros);
    }

    // ============================================================
    // EliminarTablero
    // ============================================================

    [Fact]
    public async Task EliminarTablero_CuandoExiste_LoElimina()
    {
        await using var db = CrearContexto();

        db.PowerBiTableros.Add(new PowerBiTablero
        {
            Id = 5,
            Nombre = "Ventas",
            Url = "https://app.powerbi.com/view?r=abc",
        });

        await db.SaveChangesAsync();

        var resultado = await new PowerBiController(db).EliminarTablero(5);

        Assert.IsType<OkObjectResult>(resultado);
        Assert.Empty(db.PowerBiTableros);
    }

    [Fact]
    public async Task EliminarTablero_CuandoNoExiste_Devuelve404()
    {
        await using var db = CrearContexto();

        var resultado = await new PowerBiController(db).EliminarTablero(999);

        Assert.IsType<NotFoundObjectResult>(resultado);
    }
}
