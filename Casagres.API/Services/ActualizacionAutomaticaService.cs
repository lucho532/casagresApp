using System.Text.Json;

namespace Casagres.API.Services;

public class ActualizacionAutomaticaService : BackgroundService
{
    private readonly GraphService _graphService;
    private readonly PipelineService _pipelineService;
    private readonly IConfiguration _configuration;

    // Revisar OneDrive cada hora
    private readonly TimeSpan _intervalo =
        TimeSpan.FromHours(1);

    public ActualizacionAutomaticaService(
        GraphService graphService,
        PipelineService pipelineService,
        IConfiguration configuration)
    {
        _graphService = graphService;
        _pipelineService = pipelineService;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("   SERVICIO DE ACTUALIZACIÓN AUTOMÁTICA");
        Console.WriteLine("========================================");
        Console.WriteLine();

        // Esperar unos segundos para permitir
        // que la API termine de arrancar
        await Task.Delay(
            TimeSpan.FromSeconds(10),
            stoppingToken
        );

        try
        {
            // Primera revisión al iniciar la API
            await RevisarActualizacion(stoppingToken);
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine(
                $"ERROR en actualización inicial: {ex.Message}"
            );
        }

        // A partir de aquí revisar cada hora
        using var timer =
            new PeriodicTimer(_intervalo);

        while (
            await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RevisarActualizacion(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine(
                    $"ERROR revisando actualización: {ex.Message}"
                );
            }
        }
    }

    private async Task RevisarActualizacion(
        CancellationToken stoppingToken)
    {
        Console.WriteLine();
        Console.WriteLine("========================================");
        Console.WriteLine("   REVISANDO DATOS DE ONEDRIVE");
        Console.WriteLine("========================================");

        Console.WriteLine(
            $"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm:ss}"
        );

        // Obtener información de los archivos de OneDrive
        var archivos =
            await _graphService.ObtenerEstadoArchivosAsync();

        stoppingToken.ThrowIfCancellationRequested();

        if (archivos.Count == 0)
        {
            Console.WriteLine();
            Console.WriteLine(
                "No se encontraron archivos para revisar."
            );

            return;
        }

        // Crear una huella de los archivos actuales
        var estadoActual =
            CrearHuella(archivos);

        // Leer la huella de la última actualización
        var estadoAnterior =
            await LeerEstadoAsync();

        // --------------------------------------------------
        // NO HAY CAMBIOS
        // --------------------------------------------------

        if (estadoAnterior == estadoActual)
        {
            Console.WriteLine();
            Console.WriteLine(
                "No se detectaron cambios en OneDrive."
            );

            Console.WriteLine(
                "No es necesario ejecutar el pipeline."
            );

            return;
        }

        // --------------------------------------------------
        // HAY CAMBIOS
        // --------------------------------------------------

        Console.WriteLine();
        Console.WriteLine(
            "SE DETECTARON CAMBIOS EN ONEDRIVE."
        );

        Console.WriteLine(
            "Ejecutando pipeline..."
        );

        try
        {
            await _pipelineService.EjecutarPipeline();

            // Solo guardar el estado después de que
            // el pipeline termine correctamente
            await GuardarEstadoAsync(estadoActual);

            Console.WriteLine();
            Console.WriteLine(
                "Actualización automática completada."
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine(
                $"La actualización falló: {ex.Message}"
            );

            Console.WriteLine(
                "El cambio NO se marcará como procesado."
            );

            Console.WriteLine(
                "Se volverá a intentar en la próxima revisión."
            );
        }
    }

    private string CrearHuella(
        List<(string Id, string Nombre, DateTimeOffset UltimaModificacion)> archivos)
    {
        var datos = archivos
            .OrderBy(x => x.Nombre)
            .Select(x => new
            {
                x.Id,
                x.Nombre,
                UltimaModificacion =
                    x.UltimaModificacion.UtcDateTime
            })
            .ToList();

        return JsonSerializer.Serialize(datos);
    }

    private async Task<string?> LeerEstadoAsync()
    {
        var ruta = ObtenerRutaEstado();

        if (!File.Exists(ruta))
        {
            return null;
        }

        return await File.ReadAllTextAsync(ruta);
    }

    private async Task GuardarEstadoAsync(
        string estado)
    {
        var ruta = ObtenerRutaEstado();

        var carpeta =
            Path.GetDirectoryName(ruta);

        if (!string.IsNullOrWhiteSpace(carpeta))
        {
            Directory.CreateDirectory(carpeta);
        }

        await File.WriteAllTextAsync(
            ruta,
            estado
        );
    }

    private string ObtenerRutaEstado()
    {
        var carpetaDatos =
            _configuration["Rutas:CarpetaDatos"];

        if (string.IsNullOrWhiteSpace(carpetaDatos))
        {
            throw new InvalidOperationException(
                "No se configuró Rutas:CarpetaDatos."
            );
        }

        return Path.Combine(
            carpetaDatos,
            "estado_actualizacion.json"
        );
    }
}