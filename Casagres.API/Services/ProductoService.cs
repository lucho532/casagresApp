using ClosedXML.Excel;

namespace Casagres.API.Services;

public class ProductoService
{
    private readonly IConfiguration _configuration;

    // =========================================
    // CACHÉ EN MEMORIA
    // =========================================

    private List<object>? _productosCache;

    private readonly object _lock = new();

    public ProductoService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // =========================================
    // OBTENER PRODUCTOS
    // =========================================

    public List<object> ObtenerProductos()
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

        // Primera vez:
        // cargar los productos desde Excel.
        var productos = CargarProductosDesdeExcel();

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
    // CARGAR DESDE EXCEL
    // =========================================

    private List<object> CargarProductosDesdeExcel()
    {
        Console.WriteLine();
        Console.WriteLine("Cargando catálogo de productos desde Excel...");

        var inicio = DateTime.Now;
        var rutaArchivo = ObtenerRutaArchivoExcel();

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

        var rutaArchivo = Path.Combine(carpetaDatos, "Ventas_Casagres_Limpio_PowerBI.xlsx");

        if (!File.Exists(rutaArchivo))
        {
            throw new FileNotFoundException($"No se encontró el archivo: {rutaArchivo}");
        }

        return rutaArchivo;
    }

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

    private static List<object> LeerProductosUnicos(IXLWorksheet worksheet, ColumnasProducto columnas)
    {
        // Clave = referencia, para descartar duplicados conservando el primero.
        var productos = new Dictionary<string, object>();

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

    private static object LeerProducto(IXLRow fila, ColumnasProducto columnas, string referencia) => new
    {
        referencia,
        descripcion = fila.Cell(columnas.Descripcion).GetString().Trim(),
        marca = fila.Cell(columnas.Marca).GetString().Trim(),
        linea = fila.Cell(columnas.Linea).GetString().Trim(),
        grupo = fila.Cell(columnas.Grupo).GetString().Trim(),
        clase = fila.Cell(columnas.Clase).GetString().Trim(),
        planta = fila.Cell(columnas.Planta).GetString().Trim()
    };

    private static void LogResultado(int cantidad, TimeSpan tiempo)
    {
        Console.WriteLine($"Catálogo cargado: {cantidad} productos.");
        Console.WriteLine($"Tiempo de carga: {tiempo.TotalSeconds:F2} segundos.");
    }

    private readonly record struct ColumnasProducto(
        int Referencia, int Descripcion, int Marca, int Linea, int Grupo, int Clase, int Planta);
}
