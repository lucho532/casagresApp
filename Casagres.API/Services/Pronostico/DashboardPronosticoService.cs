using Casagres.API.Models.Dtos.Pronostico;

namespace Casagres.API.Services.Pronostico;

public class DashboardPronosticoService : IDashboardPronosticoService
{
    private readonly RutasDatosService _rutas;

    public DashboardPronosticoService(RutasDatosService rutas)
    {
        _rutas = rutas;
    }

    public DashboardRespuesta Leer()
    {
        var rutaPronostico = _rutas.RutaSalidas("pronostico.csv");
        var rutaIntervalos = _rutas.RutaSalidas("pronostico_intervalos.csv");
        var rutaMetodos = _rutas.RutaSalidas("metodo_por_serie.csv");
        var rutaHistorico = _rutas.RutaSalidas("pronostico_historico.csv");

        AsegurarArchivosExisten(rutaPronostico, rutaIntervalos, rutaMetodos);

        var lineasPronostico = File.ReadAllLines(rutaPronostico);

        if (lineasPronostico.Length < 2)
        {
            throw new DatosNoEncontradosException(
                "pronostico.csv no contiene datos.");
        }

        var encabezados = lineasPronostico[0].Split(',');
        var intervalos = ConstruirIndiceIntervalos(File.ReadAllLines(rutaIntervalos));
        var metodos = ConstruirIndiceMetodos(File.ReadAllLines(rutaMetodos));

        var meses = ConstruirMeses(lineasPronostico, encabezados, intervalos, metodos);

        // pronostico_historico.csv es opcional: solo existe una vez que el
        // pipeline de predicción se ejecuta con la versión que lo genera.
        if (File.Exists(rutaHistorico))
        {
            meses.AddRange(ConstruirMesesHistoricos(File.ReadAllLines(rutaHistorico)));
        }

        return new DashboardRespuesta
        {
            Meses = meses.OrderBy(mes => mes.Mes).ToList()
        };
    }

    private static void AsegurarArchivosExisten(
        string rutaPronostico, string rutaIntervalos, string rutaMetodos)
    {
        if (!File.Exists(rutaPronostico))
            throw new FileNotFoundException("No se encontró pronostico.csv.");

        if (!File.Exists(rutaIntervalos))
            throw new FileNotFoundException("No se encontró pronostico_intervalos.csv.");

        if (!File.Exists(rutaMetodos))
            throw new FileNotFoundException("No se encontró metodo_por_serie.csv.");
    }

    // Clave del diccionario: "{mes}|{referencia}", porque un mismo producto
    // tiene un intervalo distinto por cada mes proyectado.
    private static Dictionary<string, IntervaloInfo> ConstruirIndiceIntervalos(string[] lineas)
    {
        var intervalos = new Dictionary<string, IntervaloInfo>(StringComparer.OrdinalIgnoreCase);

        for (int fila = 1; fila < lineas.Length; fila++)
        {
            var entrada = ExtraerIntervalo(lineas[fila]);

            if (entrada != null)
            {
                intervalos[entrada.Value.Clave] = entrada.Value.Info;
            }
        }

        return intervalos;
    }

    private static (string Clave, IntervaloInfo Info)? ExtraerIntervalo(string linea)
    {
        if (string.IsNullOrWhiteSpace(linea))
            return null;

        var valores = linea.Split(',');

        if (valores.Length < 8)
            return null;

        var mes = valores[0].Trim();
        var referencia = valores[1].Trim();

        if (string.IsNullOrWhiteSpace(mes) || string.IsNullOrWhiteSpace(referencia))
            return null;

        var inferior = CsvCampoParser.ADouble(valores[4]);
        var superior = CsvCampoParser.ADouble(valores[5]);
        var halfWidth = CsvCampoParser.ADouble(valores[6]);
        var alphaAci = CsvCampoParser.ADouble(valores[7]);

        if (inferior is null || superior is null || halfWidth is null || alphaAci is null)
            return null;

        var info = new IntervaloInfo(inferior.Value, superior.Value, halfWidth.Value, alphaAci.Value);

        return ($"{mes}|{referencia}", info);
    }

    private static Dictionary<string, MetodoInfo> ConstruirIndiceMetodos(string[] lineas)
    {
        var metodos = new Dictionary<string, MetodoInfo>(StringComparer.OrdinalIgnoreCase);

        for (int fila = 1; fila < lineas.Length; fila++)
        {
            var entrada = ExtraerMetodo(lineas[fila]);

            if (entrada != null)
            {
                metodos[entrada.Value.Referencia] = entrada.Value.Info;
            }
        }

        return metodos;
    }

    private static (string Referencia, MetodoInfo Info)? ExtraerMetodo(string linea)
    {
        if (string.IsNullOrWhiteSpace(linea))
            return null;

        var valores = linea.Split(',');

        if (valores.Length < 5)
            return null;

        var referencia = valores[0].Trim();

        if (string.IsNullOrWhiteSpace(referencia))
            return null;

        var tieneHiperparametros = CsvCampoParser.ABooleano(valores[2]);
        var nScoresAci = CsvCampoParser.AEntero(valores[3]);
        var alphaAci = CsvCampoParser.ADouble(valores[4]);

        if (tieneHiperparametros is null || nScoresAci is null || alphaAci is null)
            return null;

        var info = new MetodoInfo(valores[1].Trim(), tieneHiperparametros.Value, nScoresAci.Value, alphaAci.Value);

        return (referencia, info);
    }

    private static List<DashboardMes> ConstruirMeses(
        string[] lineasPronostico,
        string[] encabezados,
        Dictionary<string, IntervaloInfo> intervalos,
        Dictionary<string, MetodoInfo> metodos)
    {
        var meses = new List<DashboardMes>();

        for (int fila = 1; fila < lineasPronostico.Length; fila++)
        {
            var mes = ConstruirMes(lineasPronostico[fila], encabezados, intervalos, metodos);

            if (mes != null)
            {
                meses.Add(mes);
            }
        }

        return meses;
    }

    private static DashboardMes? ConstruirMes(
        string linea,
        string[] encabezados,
        Dictionary<string, IntervaloInfo> intervalos,
        Dictionary<string, MetodoInfo> metodos)
    {
        if (string.IsNullOrWhiteSpace(linea))
            return null;

        var valores = linea.Split(',');

        if (valores.Length == 0)
            return null;

        var mes = valores[0].Trim();

        if (string.IsNullOrWhiteSpace(mes))
            return null;

        var productos = ConstruirProductosDelMes(mes, encabezados, valores, intervalos, metodos);

        return new DashboardMes
        {
            Mes = mes,
            CantidadProductos = productos.Count,
            Productos = productos
        };
    }

    private static List<DashboardProducto> ConstruirProductosDelMes(
        string mes,
        string[] encabezados,
        string[] valores,
        Dictionary<string, IntervaloInfo> intervalos,
        Dictionary<string, MetodoInfo> metodos)
    {
        var productos = new List<DashboardProducto>();

        for (int columna = 1; columna < encabezados.Length; columna++)
        {
            var producto = ConstruirProducto(mes, encabezados, valores, columna, intervalos, metodos);

            if (producto != null)
            {
                productos.Add(producto);
            }
        }

        return productos;
    }

    private static DashboardProducto? ConstruirProducto(
        string mes,
        string[] encabezados,
        string[] valores,
        int columna,
        Dictionary<string, IntervaloInfo> intervalos,
        Dictionary<string, MetodoInfo> metodos)
    {
        if (columna >= valores.Length)
            return null;

        var referencia = encabezados[columna].Trim();

        if (string.IsNullOrWhiteSpace(referencia))
            return null;

        var pronostico = CsvCampoParser.ADouble(valores[columna]);

        if (pronostico is null)
            return null;

        metodos.TryGetValue(referencia, out var metodoInfo);
        intervalos.TryGetValue($"{mes}|{referencia}", out var intervaloInfo);

        return new DashboardProducto
        {
            Referencia = referencia,
            Pronostico = pronostico.Value,
            Metodo = metodoInfo.Metodo,
            TieneHiperparametros = metodoInfo.TieneHiperparametros,
            NScoresAci = metodoInfo.NScoresAci,
            AlphaAci = intervaloInfo.AlphaAci,
            Inferior = intervaloInfo.Inferior,
            Superior = intervaloInfo.Superior,
            HalfWidth = intervaloInfo.HalfWidth
        };
    }

    // pronostico_historico.csv trae, mes a mes, lo que el modelo habría
    // predicho para ese mes ya observado (backtest causal usado para
    // calibrar los intervalos ACI). A diferencia de pronostico.csv, aquí
    // cada fila ya trae su propia referencia, así que se agrupa por mes.
    private static List<DashboardMes> ConstruirMesesHistoricos(string[] lineas)
    {
        var productosPorMes = new Dictionary<string, List<DashboardProducto>>();

        for (int fila = 1; fila < lineas.Length; fila++)
        {
            var entrada = ExtraerFilaHistorico(lineas[fila]);

            if (entrada is null)
            {
                continue;
            }

            var (mes, producto) = entrada.Value;

            if (!productosPorMes.TryGetValue(mes, out var productos))
            {
                productos = new List<DashboardProducto>();
                productosPorMes[mes] = productos;
            }

            productos.Add(producto);
        }

        return productosPorMes
            .Select(par => new DashboardMes
            {
                Mes = par.Key,
                CantidadProductos = par.Value.Count,
                Productos = par.Value
            })
            .ToList();
    }

    private static (string Mes, DashboardProducto Producto)? ExtraerFilaHistorico(string linea)
    {
        if (string.IsNullOrWhiteSpace(linea))
            return null;

        var valores = linea.Split(',');

        if (valores.Length < 6)
            return null;

        var mes = valores[0].Trim();
        var referencia = valores[1].Trim();
        var metodo = valores[2].Trim();

        if (string.IsNullOrWhiteSpace(mes) || string.IsNullOrWhiteSpace(referencia))
            return null;

        var prediccion = CsvCampoParser.ADouble(valores[3]);
        var inferior = CsvCampoParser.ADouble(valores[4]);
        var superior = CsvCampoParser.ADouble(valores[5]);

        if (prediccion is null)
            return null;

        var producto = new DashboardProducto
        {
            Referencia = referencia,
            Pronostico = prediccion.Value,
            Metodo = string.IsNullOrWhiteSpace(metodo) ? null : metodo,
            Inferior = inferior,
            Superior = superior
        };

        return (mes, producto);
    }

    private readonly record struct IntervaloInfo(
        double Inferior, double Superior, double HalfWidth, double AlphaAci);

    private readonly record struct MetodoInfo(
        string Metodo, bool TieneHiperparametros, int NScoresAci, double AlphaAci);
}
