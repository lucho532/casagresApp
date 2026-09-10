using Casagres.API.Data;
using Casagres.API.Models;
using Casagres.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Casagres.API.Tests.Services;

public class ExcelExportServiceTests : IDisposable
{
    private readonly string _carpetaTemporal =
        Path.Combine(Path.GetTempPath(), "casagres-tests-" + Guid.NewGuid());

    public void Dispose()
    {
        if (Directory.Exists(_carpetaTemporal))
        {
            Directory.Delete(_carpetaTemporal, recursive: true);
        }
    }

    private static CasagresDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<CasagresDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CasagresDbContext(opciones);
    }

    private static ExcelExportService CrearServicio(CasagresDbContext db, string? carpetaDatos)
    {
        var configuracion = new Mock<IConfiguration>();
        configuracion.Setup(c => c["Rutas:CarpetaDatos"]).Returns(carpetaDatos);

        return new ExcelExportService(db, configuracion.Object);
    }

    [Fact]
    public async Task GenerarCsvVentas_SinVentasEnLaBaseDeDatos_LanzaInvalidOperationException()
    {
        await using var db = CrearContexto();
        var servicio = CrearServicio(db, _carpetaTemporal);

        var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(
            servicio.GenerarCsvVentas);

        Assert.Equal("No existen ventas en la base de datos.", excepcion.Message);
    }

    [Fact]
    public async Task GenerarCsvVentas_SinCarpetaConfigurada_LanzaInvalidOperationException()
    {
        await using var db = CrearContexto();
        db.Ventas.Add(new Venta { periodo = 202501 });
        await db.SaveChangesAsync();

        var servicio = CrearServicio(db, carpetaDatos: "   ");

        var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(
            servicio.GenerarCsvVentas);

        Assert.Equal("No se configuró Rutas:CarpetaDatos en appsettings.json.", excepcion.Message);
    }

    [Fact]
    public async Task GenerarCsvVentas_ConVentasSinPeriodo_LanzaInvalidOperationException()
    {
        await using var db = CrearContexto();
        db.Ventas.Add(new Venta { periodo = null });
        await db.SaveChangesAsync();

        var servicio = CrearServicio(db, _carpetaTemporal);

        var excepcion = await Assert.ThrowsAsync<InvalidOperationException>(
            servicio.GenerarCsvVentas);

        Assert.Equal(
            "No se encontraron períodos válidos para determinar los años.",
            excepcion.Message);
    }

    [Fact]
    public async Task GenerarCsvVentas_ConVentasDeDosAños_GeneraUnArchivoPorAño()
    {
        await using var db = CrearContexto();
        db.Ventas.Add(new Venta { periodo = 202501, referencia_producto = "REF1" });
        db.Ventas.Add(new Venta { periodo = 202412, referencia_producto = "REF2" });
        await db.SaveChangesAsync();

        var servicio = CrearServicio(db, _carpetaTemporal);

        var archivos = await servicio.GenerarCsvVentas();

        Assert.Equal(2, archivos.Count);
        Assert.Contains(archivos, a => Path.GetFileName(a) == "Ventas_Casagres_2025.csv");
        Assert.Contains(archivos, a => Path.GetFileName(a) == "Ventas_Casagres_2024.csv");
    }

    [Fact]
    public async Task GenerarCsvVentas_ElCsvSeparaColumnasPorPuntoYComaYSoloIncluyeElAñoCorrespondiente()
    {
        await using var db = CrearContexto();
        db.Ventas.Add(new Venta { periodo = 202501, referencia_producto = "REF-2025" });
        db.Ventas.Add(new Venta { periodo = 202412, referencia_producto = "REF-2024" });
        await db.SaveChangesAsync();

        var servicio = CrearServicio(db, _carpetaTemporal);

        var archivos = await servicio.GenerarCsvVentas();
        var archivo2025 = archivos.Single(a => Path.GetFileName(a) == "Ventas_Casagres_2025.csv");

        var lineas = File.ReadAllLines(archivo2025);

        Assert.Equal("referencia_producto;descripcion_producto;zona_vendedor" +
            ";nombre_zona_vendedor;tercero;nombre_tercero;calificacion_tercero" +
            ";tipo_tercero;lista_de_precio;cod_ciudad;sucursal_tercero;periodo" +
            ";valor_neto;cantidad;cantidad_devolucion;valor_venta;cantidad_notas" +
            ";valor_notas;tipo;codigo_zona;canal;lista_de_precios;departamento;marca" +
            ";linea_inventarios;descripcion_linea_inventarios;grupo_inventarios" +
            ";descripcion_grupo_inventarios;cod_producto;descripcion;clase_producto" +
            ";peso;toneladas;cantidad_neta;valor_venta_neta;valor_presupuesto;dif" +
            ";precio;valor_unitario;diferencia;precios_zonas;precios_unicos" +
            ";periodo_precios;meta_diaria_de_venta;cumpli_proyect_$;actual;zona",
            lineas[0]);

        Assert.Equal(2, lineas.Length); // encabezado + 1 sola venta de 2025
        Assert.StartsWith("REF-2025;", lineas[1]);
    }

    [Fact]
    public async Task GenerarCsvVentas_EscapaValoresQueContienenPuntoYComaEntreComillas()
    {
        await using var db = CrearContexto();
        db.Ventas.Add(new Venta
        {
            periodo = 202501,
            descripcion_producto = "Tubo; reforzado"
        });
        await db.SaveChangesAsync();

        var servicio = CrearServicio(db, _carpetaTemporal);

        var archivos = await servicio.GenerarCsvVentas();
        var lineas = File.ReadAllLines(archivos.Single());

        Assert.Contains("\"Tubo; reforzado\"", lineas[1]);
    }

    [Fact]
    public async Task GenerarCsvVentas_LosCamposNulosQuedanComoTextoVacio()
    {
        await using var db = CrearContexto();
        db.Ventas.Add(new Venta { periodo = 202501, descripcion_producto = null });
        await db.SaveChangesAsync();

        var servicio = CrearServicio(db, _carpetaTemporal);

        var archivos = await servicio.GenerarCsvVentas();
        var lineas = File.ReadAllLines(archivos.Single());
        var campos = lineas[1].Split(';');

        // La segunda columna es "descripcion_producto".
        Assert.Equal("", campos[1]);
    }
}
