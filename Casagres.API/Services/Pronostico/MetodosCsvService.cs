using Casagres.API.Models.Dtos.Pronostico;

namespace Casagres.API.Services.Pronostico;

public class MetodosCsvService
{
    private const int ColumnasEsperadas = 5;

    private readonly RutasDatosService _rutas;

    public MetodosCsvService(RutasDatosService rutas)
    {
        _rutas = rutas;
    }

    public MetodosResponse Leer()
    {
        var ruta = _rutas.RutaSalidas("metodo_por_serie.csv");
        var lineas = LeerLineas(ruta);

        if (lineas.Length <= 1)
        {
            return new MetodosResponse();
        }

        var productos = ExtraerProductos(lineas);

        return new MetodosResponse
        {
            CantidadProductos = productos.Count,
            Productos = productos
        };
    }

    private static string[] LeerLineas(string ruta)
    {
        if (!File.Exists(ruta))
        {
            throw new FileNotFoundException(
                "No se encontró el archivo de métodos.", ruta);
        }

        return File.ReadAllLines(ruta);
    }

    private static List<ProductoMetodoDto> ExtraerProductos(string[] lineas)
    {
        var productos = new List<ProductoMetodoDto>();

        for (int fila = 1; fila < lineas.Length; fila++)
        {
            var producto = ExtraerProducto(lineas[fila]);

            if (producto != null)
            {
                productos.Add(producto);
            }
        }

        return productos;
    }

    private static ProductoMetodoDto? ExtraerProducto(string linea)
    {
        if (string.IsNullOrWhiteSpace(linea))
            return null;

        var valores = linea.Split(',');

        if (valores.Length < ColumnasEsperadas)
            return null;

        var referencia = valores[0].Trim();

        if (string.IsNullOrWhiteSpace(referencia))
            return null;

        var tieneHiperparametros = CsvCampoParser.ABooleano(valores[2]);
        var nScoresAci = CsvCampoParser.AEntero(valores[3]);
        var alphaAci = CsvCampoParser.ADouble(valores[4]);

        if (tieneHiperparametros is null || nScoresAci is null || alphaAci is null)
            return null;

        return new ProductoMetodoDto
        {
            Referencia = referencia,
            Metodo = valores[1].Trim(),
            TieneHiperparametros = tieneHiperparametros.Value,
            NScoresAci = nScoresAci.Value,
            AlphaAci = alphaAci.Value
        };
    }
}
