using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Casagres.API;

public class GraphService
{
    private readonly MicrosoftAuthService _authService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public GraphService(
        MicrosoftAuthService authService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _authService = authService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    // ============================================================
    // OBTENER ARCHIVOS DE ONEDRIVE SIN DESCARGARLOS
    // ============================================================

    public async Task<List<object>> ObtenerArchivosAsync()
    {
        var token = await _authService.ObtenerTokenAsync();

        var urlCompartida =
            _configuration["OneDrive:CarpetaCompartidaUrl"];

        if (string.IsNullOrWhiteSpace(urlCompartida))
        {
            throw new Exception(
                "No se encontró OneDrive:CarpetaCompartidaUrl");
        }

        // Convertir URL compartida a Share ID
        var bytes = Encoding.UTF8.GetBytes(urlCompartida);

        var base64 = Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var shareId = "u!" + base64;

        var client = _httpClientFactory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Obtener carpeta
        var urlCarpeta =
            $"https://graph.microsoft.com/v1.0/shares/{shareId}/driveItem";

        var responseCarpeta =
            await client.GetAsync(urlCarpeta);

        var contenidoCarpeta =
            await responseCarpeta.Content.ReadAsStringAsync();

        if (!responseCarpeta.IsSuccessStatusCode)
        {
            throw new Exception(
                $"Microsoft Graph respondió " +
                $"{(int)responseCarpeta.StatusCode}: " +
                contenidoCarpeta);
        }

        using var jsonCarpeta =
            JsonDocument.Parse(contenidoCarpeta);

        var rootCarpeta =
            jsonCarpeta.RootElement;

        var nombreCarpeta =
            rootCarpeta.GetProperty("name").GetString();

        var driveId =
            rootCarpeta
                .GetProperty("parentReference")
                .GetProperty("driveId")
                .GetString();

        var carpetaId =
            rootCarpeta.GetProperty("id").GetString();

        if (string.IsNullOrWhiteSpace(driveId) ||
            string.IsNullOrWhiteSpace(carpetaId))
        {
            throw new Exception(
                "No se pudo obtener el Drive ID o el ID de la carpeta.");
        }

        // Obtener elementos
        var urlChildren =
            $"https://graph.microsoft.com/v1.0/drives/" +
            $"{driveId}/items/{carpetaId}/children";

        var archivos = new List<object>();

        while (!string.IsNullOrWhiteSpace(urlChildren))
        {
            var responseChildren =
                await client.GetAsync(urlChildren);

            var contenidoChildren =
                await responseChildren.Content.ReadAsStringAsync();

            if (!responseChildren.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Microsoft Graph respondió " +
                    $"{(int)responseChildren.StatusCode}: " +
                    contenidoChildren);
            }

            using var jsonChildren =
                JsonDocument.Parse(contenidoChildren);

            var rootChildren =
                jsonChildren.RootElement;

            if (!rootChildren.TryGetProperty(
                    "value",
                    out var elementos))
            {
                break;
            }

            foreach (var elemento in elementos.EnumerateArray())
            {
                var nombre =
                    elemento.TryGetProperty(
                        "name",
                        out var nameProperty)
                        ? nameProperty.GetString()
                        : null;

                var id =
                    elemento.TryGetProperty(
                        "id",
                        out var idProperty)
                        ? idProperty.GetString()
                        : null;

                var esCarpeta =
                    elemento.TryGetProperty(
                        "folder",
                        out _);

                var esArchivo =
                    elemento.TryGetProperty(
                        "file",
                        out _);

                if (!esArchivo)
                    continue;

                archivos.Add(new
                {
                    nombre,
                    id,
                    carpeta = nombreCarpeta,
                    tipo = "archivo"
                });
            }

            if (rootChildren.TryGetProperty(
                    "@odata.nextLink",
                    out var nextLinkProperty))
            {
                urlChildren =
                    nextLinkProperty.GetString() ?? "";
            }
            else
            {
                urlChildren = "";
            }
        }

        return archivos;
    }

    // ============================================================
    // MÉTODO ORIGINAL: DESCARGAR ARCHIVOS
    // ============================================================
    public async Task<List<(string Id, string Nombre, DateTimeOffset UltimaModificacion)>> ObtenerEstadoArchivosAsync()
    {
        var token = await _authService.ObtenerTokenAsync();

        var urlCompartida =
            _configuration["OneDrive:CarpetaCompartidaUrl"];

        if (string.IsNullOrWhiteSpace(urlCompartida))
        {
            throw new Exception(
                "No se encontró OneDrive:CarpetaCompartidaUrl");
        }

        var bytes = Encoding.UTF8.GetBytes(urlCompartida);

        var base64 = Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var shareId = "u!" + base64;

        var client = _httpClientFactory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var urlCarpeta =
            $"https://graph.microsoft.com/v1.0/shares/{shareId}/driveItem";

        var responseCarpeta =
            await client.GetAsync(urlCarpeta);

        var contenidoCarpeta =
            await responseCarpeta.Content.ReadAsStringAsync();

        if (!responseCarpeta.IsSuccessStatusCode)
        {
            throw new Exception(
                $"Microsoft Graph respondió " +
                $"{(int)responseCarpeta.StatusCode}: " +
                contenidoCarpeta);
        }

        using var jsonCarpeta =
            JsonDocument.Parse(contenidoCarpeta);

        var rootCarpeta = jsonCarpeta.RootElement;

        var driveId =
            rootCarpeta
                .GetProperty("parentReference")
                .GetProperty("driveId")
                .GetString();

        var carpetaId =
            rootCarpeta.GetProperty("id").GetString();

        if (string.IsNullOrWhiteSpace(driveId) ||
            string.IsNullOrWhiteSpace(carpetaId))
        {
            throw new Exception(
                "No se pudo obtener el Drive ID o el ID de la carpeta.");
        }

        var urlChildren =
            $"https://graph.microsoft.com/v1.0/drives/" +
            $"{driveId}/items/{carpetaId}/children";

        var archivos = new List<(string, string, DateTimeOffset)>();

        while (!string.IsNullOrWhiteSpace(urlChildren))
        {
            var responseChildren =
                await client.GetAsync(urlChildren);

            var contenidoChildren =
                await responseChildren.Content.ReadAsStringAsync();

            if (!responseChildren.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Microsoft Graph respondió " +
                    $"{(int)responseChildren.StatusCode}: " +
                    contenidoChildren);
            }

            using var jsonChildren =
                JsonDocument.Parse(contenidoChildren);

            var rootChildren =
                jsonChildren.RootElement;

            if (!rootChildren.TryGetProperty(
                    "value",
                    out var elementos))
            {
                break;
            }

            foreach (var elemento in elementos.EnumerateArray())
            {
                if (!elemento.TryGetProperty("file", out _))
                    continue;

                var id =
                    elemento.GetProperty("id").GetString();

                var nombre =
                    elemento.GetProperty("name").GetString();

                var fechaTexto =
                    elemento.GetProperty("lastModifiedDateTime").GetString();

                if (string.IsNullOrWhiteSpace(id) ||
                    string.IsNullOrWhiteSpace(nombre) ||
                    string.IsNullOrWhiteSpace(fechaTexto))
                {
                    continue;
                }

                if (DateTimeOffset.TryParse(
                        fechaTexto,
                        out var fecha))
                {
                    archivos.Add(
                        (id, nombre, fecha)
                    );
                }
            }

            if (rootChildren.TryGetProperty(
                    "@odata.nextLink",
                    out var nextLinkProperty))
            {
                urlChildren =
                    nextLinkProperty.GetString() ?? "";
            }
            else
            {
                urlChildren = "";
            }
        }

        return archivos;
    }
    public async Task ProbarGraphAsync()
    {
        var token = await _authService.ObtenerTokenAsync();

        var urlCompartida =
            _configuration["OneDrive:CarpetaCompartidaUrl"];

        if (string.IsNullOrWhiteSpace(urlCompartida))
        {
            throw new Exception(
                "No se encontró OneDrive:CarpetaCompartidaUrl");
        }

        var bytes = Encoding.UTF8.GetBytes(urlCompartida);

        var base64 = Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var shareId = "u!" + base64;

        var client = _httpClientFactory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var urlCarpeta =
            $"https://graph.microsoft.com/v1.0/shares/{shareId}/driveItem";

        Console.WriteLine();
        Console.WriteLine("==========================================");
        Console.WriteLine("OBTENIENDO CARPETA DE ONE DRIVE");
        Console.WriteLine("==========================================");

        var responseCarpeta =
            await client.GetAsync(urlCarpeta);

        var contenidoCarpeta =
            await responseCarpeta.Content.ReadAsStringAsync();

        Console.WriteLine(
            $"HTTP: {(int)responseCarpeta.StatusCode} " +
            $"{responseCarpeta.StatusCode}");

        if (!responseCarpeta.IsSuccessStatusCode)
        {
            Console.WriteLine(contenidoCarpeta);

            throw new Exception(
                "No se pudo obtener la carpeta de OneDrive.");
        }

        using var jsonCarpeta =
            JsonDocument.Parse(contenidoCarpeta);

        var rootCarpeta =
            jsonCarpeta.RootElement;

        var nombreCarpeta =
            rootCarpeta.GetProperty("name").GetString();

        var driveId =
            rootCarpeta
                .GetProperty("parentReference")
                .GetProperty("driveId")
                .GetString();

        var carpetaId =
            rootCarpeta.GetProperty("id").GetString();

        Console.WriteLine($"Carpeta: {nombreCarpeta}");
        Console.WriteLine($"Drive ID: {driveId}");
        Console.WriteLine($"Carpeta ID: {carpetaId}");

        var carpetaDestino =
            Path.Combine(
                _configuration["Rutas:CarpetaDatos"]!,
                "informes");

        Directory.CreateDirectory(carpetaDestino);

        Console.WriteLine();
        Console.WriteLine($"Destino local: {carpetaDestino}");

        var urlChildren =
            $"https://graph.microsoft.com/v1.0/drives/" +
            $"{driveId}/items/{carpetaId}/children";

        int totalArchivos = 0;
        int totalCarpetas = 0;
        int pagina = 0;

        while (!string.IsNullOrWhiteSpace(urlChildren))
        {
            pagina++;

            Console.WriteLine();
            Console.WriteLine("==========================================");
            Console.WriteLine($"CONSULTANDO PÁGINA {pagina}");
            Console.WriteLine("==========================================");

            var responseChildren =
                await client.GetAsync(urlChildren);

            var contenidoChildren =
                await responseChildren.Content.ReadAsStringAsync();

            Console.WriteLine(
                $"HTTP: {(int)responseChildren.StatusCode} " +
                $"{responseChildren.StatusCode}");

            if (!responseChildren.IsSuccessStatusCode)
            {
                Console.WriteLine(contenidoChildren);

                throw new Exception(
                    "No se pudieron obtener los elementos " +
                    "de la carpeta.");
            }

            using var jsonChildren =
                JsonDocument.Parse(contenidoChildren);

            var rootChildren =
                jsonChildren.RootElement;

            if (!rootChildren.TryGetProperty(
                    "value",
                    out var elementos))
            {
                Console.WriteLine(
                    "No se encontraron elementos.");

                break;
            }

            foreach (var elemento in elementos.EnumerateArray())
            {
                var nombre =
                    elemento.TryGetProperty(
                        "name",
                        out var nameProperty)
                        ? nameProperty.GetString()
                        : null;

                if (elemento.TryGetProperty(
                        "folder",
                        out _))
                {
                    totalCarpetas++;

                    Console.WriteLine();
                    Console.WriteLine(
                        $"[CARPETA] {nombre}");

                    continue;
                }

                if (!elemento.TryGetProperty(
                        "file",
                        out _))
                {
                    Console.WriteLine();
                    Console.WriteLine(
                        $"[OTRO] {nombre}");

                    continue;
                }

                totalArchivos++;

                var archivoId =
                    elemento.GetProperty("id").GetString();

                if (string.IsNullOrWhiteSpace(nombre) ||
                    string.IsNullOrWhiteSpace(archivoId))
                {
                    Console.WriteLine(
                        "Archivo ignorado: falta nombre o ID.");

                    continue;
                }

                Console.WriteLine();
                Console.WriteLine("------------------------------------------");
                Console.WriteLine(
                    $"ARCHIVO {totalArchivos}");
                Console.WriteLine($"Nombre: {nombre}");
                Console.WriteLine($"ID:     {archivoId}");
                Console.WriteLine("------------------------------------------");

                var urlDescarga =
                    $"https://graph.microsoft.com/v1.0/drives/" +
                    $"{driveId}/items/{archivoId}/content";

                Console.WriteLine("Descargando...");

                var responseDescarga =
                    await client.GetAsync(
                        urlDescarga,
                        HttpCompletionOption.ResponseHeadersRead);

                Console.WriteLine(
                    $"HTTP descarga: " +
                    $"{(int)responseDescarga.StatusCode} " +
                    $"{responseDescarga.StatusCode}");

                if (!responseDescarga.IsSuccessStatusCode)
                {
                    var error =
                        await responseDescarga.Content
                            .ReadAsStringAsync();

                    Console.WriteLine(
                        $"ERROR descargando {nombre}");

                    Console.WriteLine(error);

                    continue;
                }

                var rutaDestino =
                    Path.Combine(
                        carpetaDestino,
                        nombre);

                await using var streamOrigen =
                    await responseDescarga.Content
                        .ReadAsStreamAsync();

                await using var streamDestino =
                    new FileStream(
                        rutaDestino,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None);

                await streamOrigen.CopyToAsync(
                    streamDestino);

                var informacion =
                    new FileInfo(rutaDestino);

                Console.WriteLine(
                    $"Guardado: {informacion.FullName}");

                Console.WriteLine(
                    $"Tamaño: {informacion.Length:N0} bytes");
            }

            if (rootChildren.TryGetProperty(
                    "@odata.nextLink",
                    out var nextLinkProperty))
            {
                urlChildren =
                    nextLinkProperty.GetString() ?? "";
            }
            else
            {
                urlChildren = "";
            }
        }

        Console.WriteLine();
        Console.WriteLine("==========================================");
        Console.WriteLine("DESCARGA FINALIZADA");
        Console.WriteLine("==========================================");
        Console.WriteLine(
            $"Archivos descargados: {totalArchivos}");
        Console.WriteLine(
            $"Carpetas encontradas:  {totalCarpetas}");
        Console.WriteLine(
            $"Carpeta destino:       {carpetaDestino}");
        Console.WriteLine("==========================================");
    }
}