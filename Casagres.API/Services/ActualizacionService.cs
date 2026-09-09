using System.Diagnostics;

namespace Casagres.API.Services;

public class ActualizacionService
{
    private readonly IConfiguration _configuration;
    private readonly GraphService _graphService;
    private readonly SemaphoreSlim _semaforo = new(1, 1);
    private readonly ActualizacionEstadoService _estadoService;
    private readonly ProductoService _productoService;

    public ActualizacionService(
        IConfiguration configuration,
        GraphService graphService,
        ActualizacionEstadoService estadoService,
        ProductoService productoService)
    {
        _configuration = configuration;
        _graphService = graphService;
        _estadoService = estadoService;
        _productoService = productoService;
    }

    public async Task EjecutarActualizacion(int horizonte = 1)
    {
        if (horizonte <= 0)
        {
            throw new ArgumentException(
                "El horizonte debe ser mayor que cero.", nameof(horizonte));
        }

        if (!await _semaforo.WaitAsync(0))
        {
            throw new InvalidOperationException("Ya hay una actualización en curso.");
        }

        _estadoService.Iniciar();

        try
        {
            LogInicioActualizacion();

            var config = ObtenerConfiguracionActualizacion();

            await DescargarDatosDeOneDriveAsync();
            await EjecutarProcesamientoAnaliticoAsync(config);
            await EjecutarAdaptadorIcmdAsync(config);

            var carpetaPreprocesados = await EjecutarPreparacionIcmdAsync(config);
            var carpetaSalidas = await EjecutarPrediccionIcmdAsync(config, carpetaPreprocesados, horizonte);

            VerificarResultado(carpetaSalidas);
            FinalizarActualizacion();

            _estadoService.Completar();
        }
        catch (Exception ex)
        {
            _estadoService.Fallar(ex.Message);
            throw;
        }
        finally
        {
            _semaforo.Release();
        }
    }

    // ============================================================
    // ETAPAS DE LA ACTUALIZACIÓN
    // ============================================================

    private ConfiguracionActualizacion ObtenerConfiguracionActualizacion()
    {
        var python = _configuration["Python:Ejecutable"];
        var proyecto = _configuration["Python:Proyecto"];
        var carpetaDatos = _configuration["Rutas:CarpetaDatos"];

        if (string.IsNullOrWhiteSpace(python))
            throw new InvalidOperationException("No se configuró Python:Ejecutable en appsettings.json.");

        if (string.IsNullOrWhiteSpace(proyecto))
            throw new InvalidOperationException("No se configuró Python:Proyecto en appsettings.json.");

        if (string.IsNullOrWhiteSpace(carpetaDatos))
            throw new InvalidOperationException("No se configuró Rutas:CarpetaDatos en appsettings.json.");

        return new ConfiguracionActualizacion(python, proyecto, carpetaDatos);
    }

    private async Task DescargarDatosDeOneDriveAsync()
    {
        _estadoService.Actualizar("Descargando archivos desde OneDrive...", 10);

        Console.WriteLine();
        Console.WriteLine("----------------------------------------");
        Console.WriteLine(" DESCARGANDO ARCHIVOS DE ONEDRIVE");
        Console.WriteLine("----------------------------------------");

        await _graphService.ProbarGraphAsync();
    }

    private async Task EjecutarProcesamientoAnaliticoAsync(ConfiguracionActualizacion config)
    {
        _estadoService.Actualizar("Procesando información de ventas...", 30);

        var script = Path.Combine(
            config.Proyecto, "src", "modulo_analitica", "procesamiento_analitica_final.py");

        await EjecutarPython(
            config.Python,
            script,
            "Procesamiento analítico",
            $"--carpeta-datos \"{config.CarpetaDatos}\"");
    }

    private async Task EjecutarAdaptadorIcmdAsync(ConfiguracionActualizacion config)
    {
        _estadoService.Actualizar("Preparando información para el modelo ICMD...", 45);

        var script = Path.Combine(config.Proyecto, "src", "modulo_prediccion", "adaptador_icmd.py");

        await EjecutarPython(
            config.Python,
            script,
            "Adaptador ICMD",
            $"--carpeta-datos \"{config.CarpetaDatos}\"");
    }

    private async Task<string> EjecutarPreparacionIcmdAsync(ConfiguracionActualizacion config)
    {
        _estadoService.Actualizar("Preparando datos y generando series mensuales...", 60);

        var script = Path.Combine(config.Proyecto, "src", "modulo_prediccion", "preparacion_icmd.py");
        var archivoPreprocesado = Path.Combine(config.CarpetaDatos, "df_preprocessed.csv");
        var carpetaPreprocesados = Path.Combine(config.CarpetaDatos, "datos_preprocesados");

        await EjecutarPython(
            config.Python,
            script,
            "Preparación ICMD",
            $"--csv \"{archivoPreprocesado}\" --salidas \"{carpetaPreprocesados}\"");

        return carpetaPreprocesados;
    }

    private async Task<string> EjecutarPrediccionIcmdAsync(
        ConfiguracionActualizacion config, string carpetaPreprocesados, int horizonte)
    {
        _estadoService.Actualizar("Ejecutando predicciones...", 80);

        var script = Path.Combine(config.Proyecto, "src", "modulo_prediccion", "prediccion_icmd.py");
        var archivoSeries = Path.Combine(carpetaPreprocesados, "series_mensuales.csv");
        var carpetaSalidas = Path.Combine(config.CarpetaDatos, "salidas_prediccion");

        await EjecutarPython(
            config.Python,
            script,
            "Predicción ICMD",
            $"--series \"{archivoSeries}\" --horizonte {horizonte} --salidas \"{carpetaSalidas}\"");

        return carpetaSalidas;
    }

    private static void VerificarResultado(string carpetaSalidas)
    {
        var rutaPronosticoFinal = Path.Combine(carpetaSalidas, "pronostico.csv");

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine(" VERIFICACIÓN RESULTADO");
        Console.WriteLine("========================================");
        Console.WriteLine($"Archivo: {rutaPronosticoFinal}");
        Console.WriteLine($"Existe: {File.Exists(rutaPronosticoFinal)}");

        if (File.Exists(rutaPronosticoFinal))
        {
            Console.WriteLine($"Última modificación: {File.GetLastWriteTime(rutaPronosticoFinal)}");
            Console.WriteLine($"Tamaño: {new FileInfo(rutaPronosticoFinal).Length} bytes");
        }
    }

    private void FinalizarActualizacion()
    {
        _estadoService.Actualizar("Generando resultados finales...", 95);

        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("       ACTUALIZACIÓN FINALIZADA");
        Console.WriteLine("========================================");
        Console.WriteLine();

        // El Excel acaba de ser actualizado: la próxima consulta de
        // productos recargará el catálogo desde Excel.
        _productoService.LimpiarCache();
    }

    private static void LogInicioActualizacion()
    {
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("       INICIO DE ACTUALIZACIÓN");
        Console.WriteLine("========================================");
    }

    // ============================================================
    // EJECUCIÓN DE SCRIPTS PYTHON
    // ============================================================

    private async Task EjecutarPython(
        string python,
        string script,
        string nombreEtapa,
        string argumentos = "")
    {
        Console.WriteLine();
        Console.WriteLine("----------------------------------------");
        Console.WriteLine($" EJECUTANDO: {nombreEtapa}");
        Console.WriteLine("----------------------------------------");

        if (!File.Exists(script))
        {
            throw new FileNotFoundException($"No se encontró el script: {script}");
        }

        using var proceso = new Process { StartInfo = ConstruirProcesoPython(python, script, argumentos) };

        proceso.OutputDataReceived += (_, e) => LogSalidaEtapa(nombreEtapa, e.Data);
        proceso.ErrorDataReceived += (_, e) => LogErrorEtapa(nombreEtapa, e.Data);

        proceso.Start();
        proceso.BeginOutputReadLine();
        proceso.BeginErrorReadLine();

        await proceso.WaitForExitAsync();

        if (proceso.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"La etapa '{nombreEtapa}' terminó con código de error: {proceso.ExitCode}");
        }

        Console.WriteLine();
        Console.WriteLine($"✓ {nombreEtapa} finalizado correctamente.");
    }

    private static ProcessStartInfo ConstruirProcesoPython(string python, string script, string argumentos) =>
        new()
        {
            FileName = python,
            Arguments = $"\"{script}\" {argumentos}",
            WorkingDirectory = Path.GetDirectoryName(script)!,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,

            Environment =
            {
                ["PYTHONIOENCODING"] = "utf-8",
                ["PYTHONUTF8"] = "1"
            }
        };

    private static void LogSalidaEtapa(string nombreEtapa, string? linea)
    {
        if (!string.IsNullOrEmpty(linea))
        {
            Console.WriteLine($"[{nombreEtapa}] {linea}");
        }
    }

    private static void LogErrorEtapa(string nombreEtapa, string? linea)
    {
        if (!string.IsNullOrEmpty(linea))
        {
            Console.WriteLine($"[{nombreEtapa}] ERROR: {linea}");
        }
    }

    private readonly record struct ConfiguracionActualizacion(string Python, string Proyecto, string CarpetaDatos);
}
