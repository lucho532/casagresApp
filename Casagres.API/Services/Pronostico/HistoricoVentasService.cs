using Casagres.API.Models.Dtos.Pronostico;

namespace Casagres.API.Services.Pronostico;

public class HistoricoVentasService
{
    private readonly RutasDatosService _rutas;

    public HistoricoVentasService(RutasDatosService rutas)
    {
        _rutas = rutas;
    }

    public HistoricoResponse Leer(string? referencia)
    {
        var ruta = _rutas.RutaDatosPreprocesados("series_mensuales.csv");
        var lineas = LeerLineas(ruta);

        if (lineas.Length < 2)
        {
            throw new DatosNoEncontradosException(
                "El archivo histórico no contiene datos.");
        }

        var encabezados = lineas[0].Split(',');
        var indiceProducto = BuscarIndiceProducto(encabezados, referencia);

        var historico = lineas
            .Skip(1)
            .Select(linea => ExtraerMes(linea, referencia, indiceProducto))
            .Where(mes => mes != null)
            .Select(mes => mes!)
            .ToList();

        return new HistoricoResponse
        {
            Referencia = referencia,
            CantidadMeses = historico.Count,
            Historico = historico
        };
    }

    private static string[] LeerLineas(string ruta)
    {
        if (!File.Exists(ruta))
        {
            throw new FileNotFoundException("No se encontró series_mensuales.csv.");
        }

        return File.ReadAllLines(ruta);
    }

    private static int BuscarIndiceProducto(string[] encabezados, string? referencia)
    {
        if (string.IsNullOrWhiteSpace(referencia))
            return -1;

        for (int columna = 1; columna < encabezados.Length; columna++)
        {
            if (encabezados[columna].Trim()
                .Equals(referencia.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return columna;
            }
        }

        throw new DatosNoEncontradosException(
            $"No se encontró la referencia '{referencia}'.");
    }

    private static HistoricoMensual? ExtraerMes(string linea, string? referencia, int indiceProducto)
    {
        if (string.IsNullOrWhiteSpace(linea))
            return null;

        var valores = linea.Split(',');

        if (valores.Length == 0)
            return null;

        var mes = valores[0].Trim();

        if (string.IsNullOrWhiteSpace(mes))
            return null;

        if (string.IsNullOrWhiteSpace(referencia))
        {
            return new HistoricoMensual { Mes = mes, Cantidad = SumarTodasLasColumnas(valores) };
        }

        // Fila más corta que la columna esperada: se descarta el mes,
        // igual que si la referencia no tuviera dato para ese periodo.
        if (indiceProducto >= valores.Length)
            return null;

        var cantidad = CsvCampoParser.ADouble(valores[indiceProducto]) ?? 0;

        return new HistoricoMensual { Mes = mes, Cantidad = cantidad };
    }

    private static double SumarTodasLasColumnas(string[] valores)
    {
        double total = 0;

        for (int columna = 1; columna < valores.Length; columna++)
        {
            total += CsvCampoParser.ADouble(valores[columna]) ?? 0;
        }

        return total;
    }
}
