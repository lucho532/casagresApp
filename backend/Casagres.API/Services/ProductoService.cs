using System.Text.Json;
using Casagres.API.Models;
using ClosedXML.Excel;

namespace Casagres.API.Services;

public class ProductoService : IProductoService
{
    private const string NombreArchivoExcel = "Ventas_Casagres_Limpio_PowerBI.xlsx";
    private const string NombreArchivoCache = "cache_catalogo_productos.json";

    private readonly IConfiguration _configuration;

    // =========================================
    // CACHÉ EN MEMORIA
    // =========================================

    private List<ProductoCatalogo>? _productosCache;

    private readonly object _lock = new();

    public ProductoService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // =========================================
    // OBTENER PRODUCTOS
    // =========================================

    public List<ProductoCatalogo> ObtenerProductos()
    {
        // Si ya tenemos los productos cargados,
        // los devolvemos inmediatamente.
        lock (_lock)
        {
            if (_productosCache != null)
            {
                return _productosCache;
            }
        }

        var productos = CargarProductos();

        // Guardar en caché.
        lock (_lock)
        {
            _productosCache ??= productos;

            return _productosCache;
        }
    }

    // =========================================
    // INVALIDAR CACHÉ
    // =========================================

    public void LimpiarCache()
    {
        lock (_lock)
        {
            _productosCache = null;
        }

        Console.WriteLine("Caché de productos eliminada.");
    }

    // =========================================
    // CARGAR PRODUCTOS (memoria -> disco -> Excel)
    // =========================================
    //
    // El Excel de ventas tiene decenas de miles de filas: parsearlo con
    // ClosedXML toma varios segundos. El caché en memoria evita repetirlo
    // en cada login mientras el proceso siga corriendo, pero un redeploy
    // (o cualquier reinicio del contenedor) lo pierde por completo. Por
    // eso también se guarda una copia liviana en disco, en la misma
    // carpeta persistente que el Excel: si el Excel no cambió desde que
    // se generó ese caché, se lee ese JSON (instantáneo) en vez de volver
    // a parsear el archivo completo.

    private List<ProductoCatalogo> CargarProductos()
    {
        var rutaExcel = ObtenerRutaArchivoExcel();
        var rutaCache = ObtenerRutaArchivoCache(rutaExcel);

        if (CacheEnDiscoEsValida(rutaExcel, rutaCache))
        {
            var productosDesdeCache = IntentarLeerCacheDeDisco(rutaCache);

            if (productosDesdeCache != null)
            {
                Console.WriteLine("Catálogo de productos cargado desde el caché en disco.");

                return productosDesdeCache;
            }
        }

        var productos = CargarProductosDesdeExcel(rutaExcel);

        GuardarCacheEnDisco(rutaCache, productos);

        return productos;
    }

    private static bool CacheEnDiscoEsValida(string rutaExcel, string rutaCache)
    {
        if (!File.Exists(rutaCache))
        {
            return false;
        }

        // Si el Excel se modificó después de generarse el caché (por
        // ejemplo, tras un "Actualizar datos"), el caché quedó obsoleto.
        return File.GetLastWriteTimeUtc(rutaExcel) <= File.GetLastWriteTimeUtc(rutaCache);
    }

    private static List<ProductoCatalogo>? IntentarLeerCacheDeDisco(string rutaCache)
    {
        try
        {
            var json = File.ReadAllText(rutaCache);

            return JsonSerializer.Deserialize<List<ProductoCatalogo>>(json);
        }
        catch (Exception ex)
        {
            // Un caché corrupto o de un formato antiguo no debe romper la
            // aplicación: simplemente se ignora y se recarga desde Excel.
            Console.WriteLine(
                $"No fue posible leer el caché de productos en disco, se recargará desde Excel: {ex.Message}");

            return null;
        }
    }

    private static void GuardarCacheEnDisco(string rutaCache, List<ProductoCatalogo> productos)
    {
        try
        {
            File.WriteAllText(rutaCache, JsonSerializer.Serialize(productos));
        }
        catch (Exception ex)
        {
            // Si no se pudo escribir el caché, no pasa nada grave: la
            // próxima vez simplemente se vuelve a parsear el Excel.
            Console.WriteLine($"No fue posible guardar el caché de productos en disco: {ex.Message}");
        }
    }

    // =========================================
    // CARGAR DESDE EXCEL
    // =========================================

    private static List<ProductoCatalogo> CargarProductosDesdeExcel(string rutaArchivo)
    {
        Console.WriteLine();
        Console.WriteLine("Cargando catálogo de productos desde Excel...");

        var inicio = DateTime.Now;

        using var workbook = new XLWorkbook(rutaArchivo);
        var worksheet = workbook.Worksheets.First();

        var columnas = ObtenerColumnas(worksheet);
        var resultado = LeerProductosUnicos(worksheet, columnas);

        LogResultado(resultado.Count, DateTime.Now - inicio);

        return resultado;
    }

    private string ObtenerRutaArchivoExcel()
    {
        var carpetaDatos = _configuration["Rutas:CarpetaDatos"];

        if (string.IsNullOrWhiteSpace(carpetaDatos))
        {
            throw new Exception("No se encontró la configuración Rutas:CarpetaDatos.");
        }

        var rutaArchivo = Path.Combine(carpetaDatos, NombreArchivoExcel);

        if (!File.Exists(rutaArchivo))
        {
            throw new FileNotFoundException($"No se encontró el archivo: {rutaArchivo}");
        }

        return rutaArchivo;
    }

    private static string ObtenerRutaArchivoCache(string rutaExcel) =>
        Path.Combine(Path.GetDirectoryName(rutaExcel)!, NombreArchivoCache);

    private static ColumnasProducto ObtenerColumnas(IXLWorksheet worksheet)
    {
        var primeraFila = worksheet.FirstRowUsed()
            ?? throw new Exception("El archivo Excel no contiene datos.");

        var encabezados = primeraFila.Cells()
            .ToDictionary(
                celda => celda.GetString().Trim().ToLower(),
                celda => celda.Address.ColumnNumber);

        int Columna(string nombre) =>
            encabezados.TryGetValue(nombre.ToLower(), out var columna)
                ? columna
                : throw new Exception($"No se encontró la columna '{nombre}' en el Excel.");

        return new ColumnasProducto(
            Referencia: Columna("referencia_producto"),
            Descripcion: Columna("descripcion_producto"),
            Marca: Columna("marca"),
            Linea: Columna("descripcion_linea_inventarios"),
            Grupo: Columna("descripcion_grupo_inventarios"),
            Clase: Columna("clase_producto"),
            Planta: Columna("planta"));
    }

    private static List<ProductoCatalogo> LeerProductosUnicos(IXLWorksheet worksheet, ColumnasProducto columnas)
    {
        // Clave = referencia, para descartar duplicados conservando el primero.
        var productos = new Dictionary<string, ProductoCatalogo>();

        foreach (var fila in worksheet.RowsUsed().Skip(1))
        {
            var referencia = fila.Cell(columnas.Referencia).GetString().Trim();

            if (string.IsNullOrWhiteSpace(referencia) || productos.ContainsKey(referencia))
                continue;

            productos[referencia] = LeerProducto(fila, columnas, referencia);
        }

        return productos
            .OrderBy(par => par.Key)
            .Select(par => par.Value)
            .ToList();
    }

    private static ProductoCatalogo LeerProducto(IXLRow fila, ColumnasProducto columnas, string referencia) => new()
    {
        Referencia = referencia,
        Descripcion = fila.Cell(columnas.Descripcion).GetString().Trim(),
        Marca = fila.Cell(columnas.Marca).GetString().Trim(),
        Linea = fila.Cell(columnas.Linea).GetString().Trim(),
        Grupo = fila.Cell(columnas.Grupo).GetString().Trim(),
        Clase = fila.Cell(columnas.Clase).GetString().Trim(),
        Planta = fila.Cell(columnas.Planta).GetString().Trim()
    };

    private static void LogResultado(int cantidad, TimeSpan tiempo)
    {
        Console.WriteLine($"Catálogo cargado: {cantidad} productos.");
        Console.WriteLine($"Tiempo de carga: {tiempo.TotalSeconds:F2} segundos.");
    }

    private readonly record struct ColumnasProducto(
        int Referencia, int Descripcion, int Marca, int Linea, int Grupo, int Clase, int Planta);
}
