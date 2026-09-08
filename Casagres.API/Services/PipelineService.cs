using System.Diagnostics;

namespace Casagres.API.Services;

public class PipelineService
{
    private readonly IConfiguration _configuration;
    private readonly GraphService _graphService;
    private readonly SemaphoreSlim _semaforo = new(1, 1);
    private readonly PipelineEstadoService _estadoService;
    private readonly ProductoService _productoService;

    public PipelineService(
        IConfiguration configuration,
        GraphService graphService,
        PipelineEstadoService estadoService,
        ProductoService productoService)
    {
        _configuration = configuration;
        _graphService = graphService;
        _estadoService = estadoService;
        _productoService = productoService;
    }

    public async Task EjecutarPipeline(int horizonte = 1)
    {
        if (horizonte <= 0)
        {
            throw new ArgumentException(
                "El horizonte debe ser mayor que cero.",
                nameof(horizonte)
            );
        }

        if (!await _semaforo.WaitAsync(0))
        {
            throw new InvalidOperationException(
                "Ya hay una actualización en curso."
            );
        }

        _estadoService.Iniciar();

        try
        {
            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine("       INICIO DE ACTUALIZACIÓN");
            Console.WriteLine("========================================");

            // --------------------------------------------------
            // 1. DESCARGAR DATOS DESDE ONEDRIVE
            // --------------------------------------------------

            _estadoService.Actualizar(
                "Descargando archivos desde OneDrive...",
                10
            );

            Console.WriteLine();
            Console.WriteLine("----------------------------------------");
            Console.WriteLine(" DESCARGANDO ARCHIVOS DE ONEDRIVE");
            Console.WriteLine("----------------------------------------");

            await _graphService.ProbarGraphAsync();


            // --------------------------------------------------
            // 2. PROCESAMIENTO ANALÍTICO
            // --------------------------------------------------

            _estadoService.Actualizar(
                "Procesando información de ventas...",
                30
            );

            var python = _configuration["Python:Ejecutable"];
            var proyecto = _configuration["Python:Proyecto"];
            var carpetaDatos = _configuration["Rutas:CarpetaDatos"];

            if (string.IsNullOrWhiteSpace(python))
                throw new InvalidOperationException(
                    "No se configuró Python:Ejecutable en appsettings.json."
                );

            if (string.IsNullOrWhiteSpace(proyecto))
                throw new InvalidOperationException(
                    "No se configuró Python:Proyecto en appsettings.json."
                );

            if (string.IsNullOrWhiteSpace(carpetaDatos))
                throw new InvalidOperationException(
                    "No se configuró Rutas:CarpetaDatos en appsettings.json."
                );

            var procesamientoAnalitica = Path.Combine(
                proyecto,
                "src",
                "Módulo de Analítica de datos",
                "procesamiento_analitica_final.py"
            );

            await EjecutarPython(
                python,
                procesamientoAnalitica,
                "Procesamiento analítico"
            );


            // --------------------------------------------------
            // 3. ADAPTADOR ICMD
            // --------------------------------------------------

            _estadoService.Actualizar(
                "Preparando información para el modelo ICMD...",
                45
            );

            var adaptadorIcmd = Path.Combine(
                proyecto,
                "src",
                "Modulo Predicción",
                "adaptador_icmd.py"
            );

            await EjecutarPython(
                python,
                adaptadorIcmd,
                "Adaptador ICMD"
            );


            // --------------------------------------------------
            // 4. PREPARACIÓN ICMD
            // --------------------------------------------------

            _estadoService.Actualizar(
                "Preparando datos y generando series mensuales...",
                60
            );

            var preparacionIcmd = Path.Combine(
                proyecto,
                "src",
                "Modulo Predicción",
                "preparacion_icmd.py"
            );

            var archivoPreprocesado = Path.Combine(
                carpetaDatos,
                "df_preprocessed.csv"
            );

            var carpetaPreprocesados = Path.Combine(
                carpetaDatos,
                "datos_preprocesados"
            );

            await EjecutarPython(
                python,
                preparacionIcmd,
                "Preparación ICMD",
                $"--csv \"{archivoPreprocesado}\" --salidas \"{carpetaPreprocesados}\""
            );


            // --------------------------------------------------
            // 5. PREDICCIÓN ICMD
            // --------------------------------------------------

            _estadoService.Actualizar(
                "Ejecutando predicciones...",
                80
            );

            var prediccionIcmd = Path.Combine(
                proyecto,
                "src",
                "Modulo Predicción",
                "prediccion_icmd.py"
            );

            var archivoSeries = Path.Combine(
                carpetaPreprocesados,
                "series_mensuales.csv"
            );

            var carpetaSalidas = Path.Combine(
                carpetaDatos,
                "salidas_prediccion"
            );

            await EjecutarPython(
                python,
                prediccionIcmd,
                "Predicción ICMD",
                $"--series \"{archivoSeries}\" --horizonte {horizonte} --salidas \"{carpetaSalidas}\""
            );
            var rutaPronosticoFinal = Path.Combine(
    carpetaSalidas,
    "pronostico.csv"
);

            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine(" VERIFICACIÓN RESULTADO");
            Console.WriteLine("========================================");
            Console.WriteLine($"Archivo: {rutaPronosticoFinal}");
            Console.WriteLine($"Existe: {File.Exists(rutaPronosticoFinal)}");

            if (File.Exists(rutaPronosticoFinal))
            {
                Console.WriteLine(
                    $"Última modificación: {File.GetLastWriteTime(rutaPronosticoFinal)}"
                );

                Console.WriteLine(
                    $"Tamaño: {new FileInfo(rutaPronosticoFinal).Length} bytes"
                );
            }

            // --------------------------------------------------
            // 6. FINALIZANDO
            // --------------------------------------------------

            _estadoService.Actualizar(
                "Generando resultados finales...",
                95
            );

            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine("       ACTUALIZACIÓN FINALIZADA");
            Console.WriteLine("========================================");
            Console.WriteLine();


            // =========================================
            // INVALIDAR CACHÉ DE PRODUCTOS
            // =========================================
            //
            // El Excel acaba de ser actualizado.
            // La próxima consulta de productos
            // volverá a cargar el catálogo desde Excel.
            //

            _productoService.LimpiarCache();


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
            throw new FileNotFoundException(
                $"No se encontró el script: {script}"
            );
        }

        var startInfo = new ProcessStartInfo
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

        using var proceso = new Process
        {
            StartInfo = startInfo
        };

        proceso.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine(
                    $"[{nombreEtapa}] {e.Data}"
                );
            }
        };

        proceso.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine(
                    $"[{nombreEtapa}] ERROR: {e.Data}"
                );
            }
        };

        proceso.Start();

        proceso.BeginOutputReadLine();
        proceso.BeginErrorReadLine();

        await proceso.WaitForExitAsync();

        if (proceso.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"La etapa '{nombreEtapa}' terminó con código de error: {proceso.ExitCode}"
            );
        }

        Console.WriteLine();
        Console.WriteLine(
            $"✓ {nombreEtapa} finalizado correctamente."
        );
    }
}