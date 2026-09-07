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

        Console.WriteLine(
            "Caché de productos eliminada."
        );
    }

    // =========================================
    // CARGAR DESDE EXCEL
    // =========================================

    private List<object> CargarProductosDesdeExcel()
    {
        Console.WriteLine();
        Console.WriteLine(
            "Cargando catálogo de productos desde Excel..."
        );

        var inicio = DateTime.Now;

        var carpetaDatos =
            _configuration["Rutas:CarpetaDatos"];

        if (string.IsNullOrWhiteSpace(carpetaDatos))
        {
            throw new Exception(
                "No se encontró la configuración Rutas:CarpetaDatos."
            );
        }

        var rutaArchivo = Path.Combine(
            carpetaDatos,
            "Ventas_Casagres_Limpio_PowerBI.xlsx"
        );

        if (!File.Exists(rutaArchivo))
        {
            throw new FileNotFoundException(
                $"No se encontró el archivo: {rutaArchivo}"
            );
        }

        using var workbook =
            new XLWorkbook(rutaArchivo);

        var worksheet =
            workbook.Worksheets.First();

        // =========================================
        // OBTENER ENCABEZADOS
        // =========================================

        var primeraFila =
            worksheet.FirstRowUsed();

        if (primeraFila == null)
        {
            throw new Exception(
                "El archivo Excel no contiene datos."
            );
        }

        var encabezados = primeraFila.Cells()
            .ToDictionary(
                celda =>
                    celda.GetString()
                        .Trim()
                        .ToLower(),
                celda => celda.Address.ColumnNumber
            );

        // =========================================
        // VALIDAR COLUMNAS
        // =========================================

        int ObtenerColumna(string nombre)
        {
            if (!encabezados.TryGetValue(
                    nombre.ToLower(),
                    out var columna))
            {
                throw new Exception(
                    $"No se encontró la columna '{nombre}' en el Excel."
                );
            }

            return columna;
        }

        var colReferencia =
            ObtenerColumna("referencia_producto");

        var colDescripcion =
            ObtenerColumna("descripcion_producto");

        var colMarca =
            ObtenerColumna("marca");

        var colLinea =
            ObtenerColumna(
                "descripcion_linea_inventarios"
            );

        var colGrupo =
            ObtenerColumna(
                "descripcion_grupo_inventarios"
            );

        var colClase =
            ObtenerColumna("clase_producto");

        var colPlanta =
            ObtenerColumna("planta");

        // =========================================
        // LEER PRODUCTOS ÚNICOS
        // =========================================

        var productos =
            new Dictionary<string, object>();

        foreach (var fila in worksheet.RowsUsed().Skip(1))
        {
            var referencia =
                fila.Cell(colReferencia)
                    .GetString()
                    .Trim();

            if (string.IsNullOrWhiteSpace(referencia))
            {
                continue;
            }

            if (productos.ContainsKey(referencia))
            {
                continue;
            }

            var descripcion =
                fila.Cell(colDescripcion)
                    .GetString()
                    .Trim();

            var marca =
                fila.Cell(colMarca)
                    .GetString()
                    .Trim();

            var linea =
                fila.Cell(colLinea)
                    .GetString()
                    .Trim();

            var grupo =
                fila.Cell(colGrupo)
                    .GetString()
                    .Trim();

            var clase =
                fila.Cell(colClase)
                    .GetString()
                    .Trim();

            var planta =
                fila.Cell(colPlanta)
                    .GetString()
                    .Trim();

            productos.Add(
                referencia,
                new
                {
                    referencia,
                    descripcion,
                    marca,
                    linea,
                    grupo,
                    clase,
                    planta
                }
            );
        }

        var resultado = productos.Values
            .OrderBy(producto =>
                producto.GetType()
                    .GetProperty("referencia")!
                    .GetValue(producto)!
                    .ToString()
            )
            .ToList();

        var tiempo =
            DateTime.Now - inicio;

        Console.WriteLine(
            $"Catálogo cargado: {resultado.Count} productos."
        );

        Console.WriteLine(
            $"Tiempo de carga: {tiempo.TotalSeconds:F2} segundos."
        );

        return resultado;
    }
}