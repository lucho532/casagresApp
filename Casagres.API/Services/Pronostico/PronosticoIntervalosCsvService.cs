using Casagres.API.Models.Dtos.Pronostico;

namespace Casagres.API.Services.Pronostico;

public class PronosticoIntervalosCsvService : IPronosticoIntervalosCsvService
{
    private const int ColumnasEsperadas = 8;

    private readonly RutasDatosService _rutas;

    public PronosticoIntervalosCsvService(RutasDatosService rutas)
    {
        _rutas = rutas;
    }

    public PronosticoIntervalosResponse Leer()
    {
        var ruta = _rutas.RutaSalidas("pronostico_intervalos.csv");
        var lineas = LeerLineas(ruta);

        if (lineas.Length <= 1)
        {
            return new PronosticoIntervalosResponse();
        }

        var productos = ExtraerProductos(lineas);

        return new PronosticoIntervalosResponse
        {
            Mes = productos.Count > 0 ? lineas[1].Split(',')[0].Trim() : null,
            CantidadProductos = productos.Count,
            Productos = productos
        };
    }

    private static string[] LeerLineas(string ruta)
    {
        if (!File.Exists(ruta))
        {
            throw new FileNotFoundException(
                "No se encontró el archivo de intervalos.", ruta);
        }

        return File.ReadAllLines(ruta);
    }

    private static List<ProductoIntervaloDto> ExtraerProductos(string[] lineas)
    {
        var productos = new List<ProductoIntervaloDto>();

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

    private static ProductoIntervaloDto? ExtraerProducto(string linea)
    {
        if (string.IsNullOrWhiteSpace(linea))
            return null;

        var valores = linea.Split(',');

        if (valores.Length < ColumnasEsperadas)
            return null;

        var prediccion = CsvCampoParser.ADouble(valores[3]);
        var inferior = CsvCampoParser.ADouble(valores[4]);
        var superior = CsvCampoParser.ADouble(valores[5]);
        var halfWidth = CsvCampoParser.ADouble(valores[6]);
        var alphaAci = CsvCampoParser.ADouble(valores[7]);

        if (prediccion is null || inferior is null || superior is null ||
            halfWidth is null || alphaAci is null)
        {
            return null;
        }

        return new ProductoIntervaloDto
        {
            Referencia = valores[1].Trim(),
            Metodo = valores[2].Trim(),
            Prediccion = prediccion.Value,
            Inferior = inferior.Value,
            Superior = superior.Value,
            HalfWidth = halfWidth.Value,
            AlphaAci = alphaAci.Value
        };
    }
}
