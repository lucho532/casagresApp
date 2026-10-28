using Casagres.API.Services;
using ClosedXML.Excel;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Casagres.API.Tests.Services;

public class ProductoServiceTests : IDisposable
{
    private readonly string _carpetaTemporal;

    private static readonly string[] Encabezados =
    {
        "referencia_producto", "descripcion_producto", "marca",
        "descripcion_linea_inventarios", "descripcion_grupo_inventarios",
        "clase_producto", "planta"
    };

    public ProductoServiceTests()
    {
        _carpetaTemporal = Path.Combine(Path.GetTempPath(), "casagres-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_carpetaTemporal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_carpetaTemporal))
        {
            Directory.Delete(_carpetaTemporal, recursive: true);
        }
    }

    private ProductoService CrearServicio()
    {
        var configuracion = new Mock<IConfiguration>();
        configuracion.Setup(c => c["Rutas:CarpetaDatos"]).Returns(_carpetaTemporal);

        return new ProductoService(configuracion.Object);
    }

    private void EscribirExcel(string[] encabezados, params string[][] filas)
    {
        using var workbook = new XLWorkbook();
        var hoja = workbook.Worksheets.Add("Datos");

        for (int columna = 0; columna < encabezados.Length; columna++)
        {
            hoja.Cell(1, columna + 1).Value = encabezados[columna];
        }

        for (int fila = 0; fila < filas.Length; fila++)
        {
            for (int columna = 0; columna < filas[fila].Length; columna++)
            {
                hoja.Cell(fila + 2, columna + 1).Value = filas[fila][columna];
            }
        }

        workbook.SaveAs(Path.Combine(_carpetaTemporal, "Ventas_Casagres_Limpio_PowerBI.xlsx"));
    }

    [Fact]
    public void ObtenerProductos_CuandoNoExisteElArchivo_LanzaFileNotFoundException()
    {
        var servicio = CrearServicio();

        Assert.Throws<FileNotFoundException>(() => servicio.ObtenerProductos());
    }

    [Fact]
    public void ObtenerProductos_ConDatosValidos_DevuelveLosProductosOrdenadosPorReferencia()
    {
        EscribirExcel(
            Encabezados,
            new[] { "REF2", "Producto 2", "Marca B", "Linea B", "Grupo B", "Clase B", "Planta B" },
            new[] { "REF1", "Producto 1", "Marca A", "Linea A", "Grupo A", "Clase A", "Planta A" });

        var productos = CrearServicio().ObtenerProductos();

        Assert.Equal(2, productos.Count);
        Assert.Equal("REF1", productos[0].Referencia);
        Assert.Equal("REF2", productos[1].Referencia);
    }

    [Fact]
    public void ObtenerProductos_ConReferenciasDuplicadas_ConservaSoloLaPrimeraAparicion()
    {
        EscribirExcel(
            Encabezados,
            new[] { "REF1", "Primera descripción", "Marca A", "Linea A", "Grupo A", "Clase A", "Planta A" },
            new[] { "REF1", "Segunda descripción", "Marca B", "Linea B", "Grupo B", "Clase B", "Planta B" });

        var productos = CrearServicio().ObtenerProductos();

        var producto = Assert.Single(productos);
        Assert.Equal("Primera descripción", producto.Descripcion);
    }

    [Fact]
    public void ObtenerProductos_IgnoraFilasConReferenciaVacia()
    {
        EscribirExcel(
            Encabezados,
            new[] { "", "Sin referencia", "Marca A", "Linea A", "Grupo A", "Clase A", "Planta A" },
            new[] { "REF1", "Con referencia", "Marca A", "Linea A", "Grupo A", "Clase A", "Planta A" });

        var productos = CrearServicio().ObtenerProductos();

        Assert.Single(productos);
    }

    [Fact]
    public void ObtenerProductos_CuandoFaltaUnaColumnaObligatoria_LanzaExcepcion()
    {
        EscribirExcel(
            new[] { "referencia_producto", "descripcion_producto" },
            new[] { "REF1", "Producto 1" });

        Assert.Throws<Exception>(() => CrearServicio().ObtenerProductos());
    }

    [Fact]
    public void ObtenerProductos_SegundaLlamada_UsaCacheYNoVuelveALeerElArchivo()
    {
        EscribirExcel(
            Encabezados,
            new[] { "REF1", "Producto 1", "Marca A", "Linea A", "Grupo A", "Clase A", "Planta A" });

        var servicio = CrearServicio();
        var primeraLlamada = servicio.ObtenerProductos();

        File.Delete(Path.Combine(_carpetaTemporal, "Ventas_Casagres_Limpio_PowerBI.xlsx"));

        var segundaLlamada = servicio.ObtenerProductos();

        Assert.Same(primeraLlamada, segundaLlamada);
    }

    [Fact]
    public void LimpiarCache_ObligaARecargarDesdeElArchivoEnLaSiguienteLlamada()
    {
        EscribirExcel(
            Encabezados,
            new[] { "REF1", "Producto 1", "Marca A", "Linea A", "Grupo A", "Clase A", "Planta A" });

        var servicio = CrearServicio();
        servicio.ObtenerProductos();

        servicio.LimpiarCache();
        File.Delete(Path.Combine(_carpetaTemporal, "Ventas_Casagres_Limpio_PowerBI.xlsx"));

        Assert.Throws<FileNotFoundException>(() => servicio.ObtenerProductos());
    }

    // ============================================================
    // CACHÉ EN DISCO (sobrevive a un reinicio del backend)
    // ============================================================

    [Fact]
    public void ObtenerProductos_ConUnaInstanciaNueva_UsaElCacheEnDiscoSiElExcelNoCambio()
    {
        var rutaExcel = Path.Combine(_carpetaTemporal, "Ventas_Casagres_Limpio_PowerBI.xlsx");

        EscribirExcel(
            Encabezados,
            new[] { "REF1", "Producto 1", "Marca A", "Linea A", "Grupo A", "Clase A", "Planta A" });

        CrearServicio().ObtenerProductos();

        // Simula un reinicio del backend (una instancia nueva, sin caché en
        // memoria) dejando el Excel dañado, pero con una fecha de
        // modificación anterior a la del caché: si de verdad usa el caché
        // en disco, ni siquiera debería intentar volver a parsear este
        // archivo corrupto.
        File.WriteAllText(rutaExcel, "esto no es un excel válido");
        File.SetLastWriteTimeUtc(rutaExcel, DateTime.UtcNow.AddDays(-1));

        var productos = CrearServicio().ObtenerProductos();

        var producto = Assert.Single(productos);
        Assert.Equal("REF1", producto.Referencia);
    }

    [Fact]
    public void ObtenerProductos_ConUnaInstanciaNueva_IgnoraElCacheEnDiscoSiElExcelCambioDespues()
    {
        var rutaExcel = Path.Combine(_carpetaTemporal, "Ventas_Casagres_Limpio_PowerBI.xlsx");

        EscribirExcel(
            Encabezados,
            new[] { "REF1", "Producto viejo", "Marca A", "Linea A", "Grupo A", "Clase A", "Planta A" });

        CrearServicio().ObtenerProductos();

        EscribirExcel(
            Encabezados,
            new[] { "REF1", "Producto nuevo", "Marca A", "Linea A", "Grupo A", "Clase A", "Planta A" });

        // Fuerza que el Excel quede con una fecha de modificación
        // posterior a la del caché, sin depender de la resolución del
        // reloj del sistema de archivos.
        File.SetLastWriteTimeUtc(rutaExcel, DateTime.UtcNow.AddMinutes(5));

        var productos = CrearServicio().ObtenerProductos();

        var producto = Assert.Single(productos);
        Assert.Equal("Producto nuevo", producto.Descripcion);
    }
}
