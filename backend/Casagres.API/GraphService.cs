using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Casagres.API;

public class GraphService : IGraphService
{
    private const string GraphBaseUrl = "https://graph.microsoft.com/v1.0";

    private readonly IMicrosoftAuthService _authService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public GraphService(
        IMicrosoftAuthService authService,
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
        var (client, carpeta) = await ConectarCarpetaCompartidaAsync();

        var archivos = new List<object>();

        await foreach (var elemento in EnumerarElementosAsync(client, carpeta.DriveId, carpeta.CarpetaId))
        {
            if (!elemento.TryGetProperty("file", out _))
                continue;

            archivos.Add(new
            {
                nombre = ObtenerString(elemento, "name"),
                id = ObtenerString(elemento, "id"),
                carpeta = carpeta.Nombre,
                tipo = "archivo"
            });
        }

        return archivos;
    }

    // ============================================================
    // OBTENER ESTADO (ID, NOMBRE, FECHA) DE LOS ARCHIVOS
    // ============================================================

    public async Task<List<(string Id, string Nombre, DateTimeOffset UltimaModificacion)>> ObtenerEstadoArchivosAsync()
    {
        var (client, carpeta) = await ConectarCarpetaCompartidaAsync();

        var archivos = new List<(string, string, DateTimeOffset)>();

        await foreach (var elemento in EnumerarElementosAsync(client, carpeta.DriveId, carpeta.CarpetaId))
        {
            var archivo = ExtraerEstadoArchivo(elemento);

            if (archivo != null)
            {
                archivos.Add(archivo.Value);
            }
        }

        return archivos;
    }

    private static (string, string, DateTimeOffset)? ExtraerEstadoArchivo(JsonElement elemento)
    {
        if (!elemento.TryGetProperty("file", out _))
            return null;

        var id = elemento.GetProperty("id").GetString();
        var nombre = elemento.GetProperty("name").GetString();
        var fechaTexto = elemento.GetProperty("lastModifiedDateTime").GetString();

        if (string.IsNullOrWhiteSpace(id) ||
            string.IsNullOrWhiteSpace(nombre) ||
            string.IsNullOrWhiteSpace(fechaTexto))
        {
            return null;
        }

        return DateTimeOffset.TryParse(fechaTexto, out var fecha)
            ? (id, nombre, fecha)
            : null;
    }

    // ============================================================
    // DESCARGAR TODOS LOS ARCHIVOS DE LA CARPETA COMPARTIDA
    // ============================================================

    public async Task ProbarGraphAsync()
    {
        var shareId = ConstruirShareId(ObtenerUrlCompartida());
        var client = await CrearClienteAutenticadoAsync();

        Console.WriteLine();
        Console.WriteLine("==========================================");
        Console.WriteLine("OBTENIENDO CARPETA DE ONE DRIVE");
        Console.WriteLine("==========================================");

        var carpeta = await ObtenerCarpetaRaizConLogAsync(client, shareId);

        Console.WriteLine($"Carpeta: {carpeta.Nombre}");
        Console.WriteLine($"Drive ID: {carpeta.DriveId}");
        Console.WriteLine($"Carpeta ID: {carpeta.CarpetaId}");

        var carpetaDatos = _configuration["Rutas:CarpetaDatos"]!;
        var carpetaDestino = Path.Combine(carpetaDatos, "informes");
        Directory.CreateDirectory(carpetaDestino);

        Console.WriteLine();
        Console.WriteLine($"Destino local: {carpetaDestino}");

        // Solo se descargan los archivos que sean nuevos o hayan cambiado
        // desde la última ejecución (según "lastModifiedDateTime" de
        // OneDrive); el resto se deja tal cual está en disco. Esto evita
        // volver a bajar años completos que ya están cerrados y no
        // cambian, cada vez que se pide una actualización.
        var rutaManifiesto = Path.Combine(carpetaDatos, "estado_descargas_onedrive.json");
        var manifiestoAnterior = LeerManifiesto(rutaManifiesto);
        var manifiestoNuevo = new Dictionary<string, DateTimeOffset>();

        var resumen = await DescargarCarpetaAsync(
            client, carpeta.DriveId, carpeta.CarpetaId, carpetaDestino,
            manifiestoAnterior, manifiestoNuevo);

        GuardarManifiesto(rutaManifiesto, manifiestoNuevo);

        Console.WriteLine();
        Console.WriteLine("==========================================");
        Console.WriteLine("DESCARGA FINALIZADA");
        Console.WriteLine("==========================================");
        Console.WriteLine($"Archivos descargados:      {resumen.Descargados}");
        Console.WriteLine($"Sin cambios (se omitieron): {resumen.Omitidos}");
        Console.WriteLine($"Carpetas encontradas:      {resumen.TotalCarpetas}");
        Console.WriteLine($"Carpeta destino:           {carpetaDestino}");
        Console.WriteLine("==========================================");
    }

    private async Task<ResumenDescarga> DescargarCarpetaAsync(
        HttpClient client, string driveId, string carpetaId, string carpetaDestino,
        IReadOnlyDictionary<string, DateTimeOffset> manifiestoAnterior,
        Dictionary<string, DateTimeOffset> manifiestoNuevo)
    {
        var url = $"{GraphBaseUrl}/drives/{driveId}/items/{carpetaId}/children";

        int descargados = 0;
        int omitidos = 0;
        int totalCarpetas = 0;
        int pagina = 0;

        while (!string.IsNullOrWhiteSpace(url))
        {
            pagina++;
            LogEncabezadoPagina(pagina);

            using var json = await ObtenerJsonAsync(
                client, url, "No se pudieron obtener los elementos de la carpeta.");

            var root = json.RootElement;

            if (!root.TryGetProperty("value", out var elementos))
            {
                Console.WriteLine("No se encontraron elementos.");
                break;
            }

            foreach (var elemento in elementos.EnumerateArray())
            {
                var resultado = await ProcesarElementoDescargaAsync(
                    client, elemento, driveId, carpetaDestino, descargados + omitidos + 1,
                    manifiestoAnterior, manifiestoNuevo);

                switch (resultado)
                {
                    case ResultadoElemento.Descargado:
                        descargados++;
                        break;
                    case ResultadoElemento.Omitido:
                        omitidos++;
                        break;
                    case ResultadoElemento.Carpeta:
                        totalCarpetas++;
                        break;
                }
            }

            url = ObtenerSiguientePagina(root);
        }

        return new ResumenDescarga(descargados, omitidos, totalCarpetas);
    }

    private static async Task<ResultadoElemento> ProcesarElementoDescargaAsync(
        HttpClient client, JsonElement elemento, string driveId, string carpetaDestino, int numeroArchivo,
        IReadOnlyDictionary<string, DateTimeOffset> manifiestoAnterior,
        Dictionary<string, DateTimeOffset> manifiestoNuevo)
    {
        var nombre = ObtenerString(elemento, "name");

        if (elemento.TryGetProperty("folder", out _))
        {
            Console.WriteLine();
            Console.WriteLine($"[CARPETA] {nombre}");
            return ResultadoElemento.Carpeta;
        }

        if (!elemento.TryGetProperty("file", out _))
        {
            Console.WriteLine();
            Console.WriteLine($"[OTRO] {nombre}");
            return ResultadoElemento.Otro;
        }

        return await DescargarArchivoDeElementoAsync(
            client, elemento, nombre, driveId, carpetaDestino, numeroArchivo,
            manifiestoAnterior, manifiestoNuevo);
    }

    private static async Task<ResultadoElemento> DescargarArchivoDeElementoAsync(
        HttpClient client, JsonElement elemento, string? nombre, string driveId,
        string carpetaDestino, int numeroArchivo,
        IReadOnlyDictionary<string, DateTimeOffset> manifiestoAnterior,
        Dictionary<string, DateTimeOffset> manifiestoNuevo)
    {
        var archivoId = elemento.GetProperty("id").GetString();

        if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(archivoId))
        {
            Console.WriteLine("Archivo ignorado: falta nombre o ID.");
            return ResultadoElemento.Otro;
        }

        var rutaDestino = Path.Combine(carpetaDestino, nombre);
        var fechaModificacion = ObtenerFechaModificacion(elemento);

        if (fechaModificacion is { } fecha)
        {
            manifiestoNuevo[nombre] = fecha;

            var sinCambios =
                File.Exists(rutaDestino) &&
                manifiestoAnterior.TryGetValue(nombre, out var fechaConocida) &&
                fechaConocida == fecha;

            if (sinCambios)
            {
                Console.WriteLine();
                Console.WriteLine($"[SIN CAMBIOS] {nombre} (se omite, ya está actualizado)");
                return ResultadoElemento.Omitido;
            }
        }

        Console.WriteLine();
        Console.WriteLine("------------------------------------------");
        Console.WriteLine($"ARCHIVO {numeroArchivo}");
        Console.WriteLine($"Nombre: {nombre}");
        Console.WriteLine($"ID:     {archivoId}");
        Console.WriteLine("------------------------------------------");

        await DescargarArchivoAsync(client, driveId, archivoId, nombre, carpetaDestino);

        return ResultadoElemento.Descargado;
    }

    private static DateTimeOffset? ObtenerFechaModificacion(JsonElement elemento)
    {
        if (!elemento.TryGetProperty("lastModifiedDateTime", out var valor))
        {
            return null;
        }

        var texto = valor.GetString();

        return !string.IsNullOrWhiteSpace(texto) && DateTimeOffset.TryParse(texto, out var fecha)
            ? fecha
            : null;
    }

    private static Dictionary<string, DateTimeOffset> LeerManifiesto(string ruta)
    {
        if (!File.Exists(ruta))
        {
            return new Dictionary<string, DateTimeOffset>();
        }

        try
        {
            var json = File.ReadAllText(ruta);

            return JsonSerializer.Deserialize<Dictionary<string, DateTimeOffset>>(json)
                ?? new Dictionary<string, DateTimeOffset>();
        }
        catch (JsonException)
        {
            // Manifiesto corrupto o de un formato antiguo: no es un error
            // fatal, simplemente se vuelve a descargar todo esta vez.
            return new Dictionary<string, DateTimeOffset>();
        }
    }

    private static void GuardarManifiesto(string ruta, Dictionary<string, DateTimeOffset> manifiesto)
    {
        File.WriteAllText(ruta, JsonSerializer.Serialize(manifiesto));
    }

    private static async Task DescargarArchivoAsync(
        HttpClient client, string driveId, string archivoId, string nombre, string carpetaDestino)
    {
        var urlDescarga = $"{GraphBaseUrl}/drives/{driveId}/items/{archivoId}/content";

        Console.WriteLine("Descargando...");

        var response = await client.GetAsync(urlDescarga, HttpCompletionOption.ResponseHeadersRead);

        Console.WriteLine($"HTTP descarga: {(int)response.StatusCode} {response.StatusCode}");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"ERROR descargando {nombre}");
            Console.WriteLine(error);
            return;
        }

        var rutaDestino = Path.Combine(carpetaDestino, nombre);

        try
        {
            await using var streamOrigen = await response.Content.ReadAsStreamAsync();
            await using var streamDestino = new FileStream(
                rutaDestino, FileMode.Create, FileAccess.Write, FileShare.None);

            await streamOrigen.CopyToAsync(streamDestino);
        }
        catch (IOException ex)
        {
            // La causa más común: alguien tiene el archivo abierto en Excel
            // (FileShare.None impide escribirlo mientras tanto).
            throw new IOException(
                $"No se pudo guardar '{nombre}': el archivo está abierto en otro programa " +
                "(por ejemplo, Excel). Ciérralo e inténtalo de nuevo.",
                ex);
        }

        var informacion = new FileInfo(rutaDestino);
        Console.WriteLine($"Guardado: {informacion.FullName}");
        Console.WriteLine($"Tamaño: {informacion.Length:N0} bytes");
    }

    private static void LogEncabezadoPagina(int pagina)
    {
        Console.WriteLine();
        Console.WriteLine("==========================================");
        Console.WriteLine($"CONSULTANDO PÁGINA {pagina}");
        Console.WriteLine("==========================================");
    }

    private async Task<CarpetaRaiz> ObtenerCarpetaRaizConLogAsync(HttpClient client, string shareId)
    {
        var url = $"{GraphBaseUrl}/shares/{shareId}/driveItem";

        var response = await client.GetAsync(url);
        var contenido = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"HTTP: {(int)response.StatusCode} {response.StatusCode}");

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine(contenido);
            throw new Exception("No se pudo obtener la carpeta de OneDrive.");
        }

        using var json = JsonDocument.Parse(contenido);

        return LeerCarpetaRaiz(json.RootElement);
    }

    // ============================================================
    // HELPERS COMPARTIDOS
    // ============================================================

    private async Task<(HttpClient Client, CarpetaRaiz Carpeta)> ConectarCarpetaCompartidaAsync()
    {
        var shareId = ConstruirShareId(ObtenerUrlCompartida());
        var client = await CrearClienteAutenticadoAsync();
        var carpeta = await ObtenerCarpetaRaizAsync(client, shareId);

        return (client, carpeta);
    }

    private string ObtenerUrlCompartida()
    {
        var urlCompartida = _configuration["OneDrive:CarpetaCompartidaUrl"];

        if (string.IsNullOrWhiteSpace(urlCompartida))
        {
            throw new Exception("No se encontró OneDrive:CarpetaCompartidaUrl");
        }

        return urlCompartida;
    }

    private static string ConstruirShareId(string urlCompartida)
    {
        var bytes = Encoding.UTF8.GetBytes(urlCompartida);

        var base64 = Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        return "u!" + base64;
    }

    private async Task<HttpClient> CrearClienteAutenticadoAsync()
    {
        var token = await _authService.ObtenerTokenAsync();

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    private static async Task<CarpetaRaiz> ObtenerCarpetaRaizAsync(HttpClient client, string shareId)
    {
        var url = $"{GraphBaseUrl}/shares/{shareId}/driveItem";

        using var json = await ObtenerJsonAsync(client, url, "Microsoft Graph respondió con error");

        return LeerCarpetaRaiz(json.RootElement);
    }

    private static CarpetaRaiz LeerCarpetaRaiz(JsonElement root)
    {
        var nombre = root.GetProperty("name").GetString();

        var driveId = root
            .GetProperty("parentReference")
            .GetProperty("driveId")
            .GetString();

        var carpetaId = root.GetProperty("id").GetString();

        if (string.IsNullOrWhiteSpace(driveId) || string.IsNullOrWhiteSpace(carpetaId))
        {
            throw new Exception("No se pudo obtener el Drive ID o el ID de la carpeta.");
        }

        return new CarpetaRaiz(nombre ?? "", driveId, carpetaId);
    }

    // Recorre todas las páginas de "children" de una carpeta de OneDrive,
    // devolviendo cada elemento (archivo o carpeta) de forma perezosa.
    private static async IAsyncEnumerable<JsonElement> EnumerarElementosAsync(
        HttpClient client, string driveId, string carpetaId)
    {
        var url = $"{GraphBaseUrl}/drives/{driveId}/items/{carpetaId}/children";

        while (!string.IsNullOrWhiteSpace(url))
        {
            using var json = await ObtenerJsonAsync(
                client, url, "Microsoft Graph respondió con error");

            var root = json.RootElement;

            if (!root.TryGetProperty("value", out var elementos))
                yield break;

            foreach (var elemento in elementos.EnumerateArray())
            {
                yield return elemento.Clone();
            }

            url = ObtenerSiguientePagina(root);
        }
    }

    private static string ObtenerSiguientePagina(JsonElement root) =>
        root.TryGetProperty("@odata.nextLink", out var nextLink)
            ? nextLink.GetString() ?? ""
            : "";

    private static async Task<JsonDocument> ObtenerJsonAsync(HttpClient client, string url, string mensajeError)
    {
        var response = await client.GetAsync(url);
        var contenido = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"{mensajeError} ({(int)response.StatusCode}): {contenido}");
        }

        return JsonDocument.Parse(contenido);
    }

    private static string? ObtenerString(JsonElement elemento, string propiedad) =>
        elemento.TryGetProperty(propiedad, out var valor) ? valor.GetString() : null;

    private readonly record struct CarpetaRaiz(string Nombre, string DriveId, string CarpetaId);

    private readonly record struct ResumenDescarga(int Descargados, int Omitidos, int TotalCarpetas);

    private enum ResultadoElemento { Descargado, Omitido, Carpeta, Otro }
}
