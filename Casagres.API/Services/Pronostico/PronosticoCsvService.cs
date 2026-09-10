using Casagres.API.Models.Dtos.Pronostico;

namespace Casagres.API.Services.Pronostico;

public class PronosticoCsvService : IPronosticoCsvService
{
    private readonly RutasDatosService _rutas;

    public PronosticoCsvService(RutasDatosService rutas)
    {
        _rutas = rutas;
    }

    public PronosticoResponse Leer()
    {
        var ruta = _rutas.RutaSalidas("pronostico.csv");
        var lineas = LeerLineas(ruta);

        if (lineas.Length <= 1)
        {
            return new PronosticoResponse();
        }

        var encabezados = lineas[0].Split(',');
        var valores = lineas[1].Split(',');
        var productos = ExtraerProductos(encabezados, valores);

        return new PronosticoResponse
        {
            Mes = valores.Length > 0 ? valores[0] : null,
            CantidadProductos = productos.Count,
            Productos = productos
        };
    }

    private static string[] LeerLineas(string ruta)
    {
        if (!File.Exists(ruta))
        {
            throw new FileNotFoundException(
                "No se encontró el archivo de pronóstico.", ruta);
        }

        return File.ReadAllLines(ruta);
    }

    private static List<ProductoPronosticoDto> ExtraerProductos(
        string[] encabezados, string[] valores)
    {
        var productos = new List<ProductoPronosticoDto>();

        for (int columna = 1; columna < encabezados.Length; columna++)
        {
            var producto = ExtraerProducto(encabezados, valores, columna);

            if (producto != null)
            {
                productos.Add(producto);
            }
        }

        return productos;
    }

    private static ProductoPronosticoDto? ExtraerProducto(
        string[] encabezados, string[] valores, int columna)
    {
        if (columna >= valores.Length)
            return null;

        var referencia = encabezados[columna].Trim();

        if (string.IsNullOrWhiteSpace(referencia))
            return null;

        var pronostico = CsvCampoParser.ADouble(valores[columna]);

        // No enviamos productos sin pronóstico válido o en cero.
        if (pronostico is null or 0)
            return null;

        return new ProductoPronosticoDto
        {
            Referencia = referencia,
            Pronostico = pronostico.Value
        };
    }
}
