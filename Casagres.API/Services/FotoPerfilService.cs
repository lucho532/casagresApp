using Casagres.API.Data;

namespace Casagres.API.Services;

// Guarda la foto de perfil que el usuario sube manualmente como un archivo
// en la misma carpeta persistente que usa el resto de la aplicación
// (Rutas:CarpetaDatos), en vez de en la base de datos: evita inflar filas
// de la tabla de usuarios con contenido binario.
public class FotoPerfilService : IFotoPerfilService
{
    private const string NombreCarpeta = "fotos_perfil";

    private const long TamanoMaximoBytes = 3 * 1024 * 1024; // 3 MB

    private static readonly Dictionary<string, string> ExtensionesPorContentType = new()
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
    };

    private readonly CasagresDbContext _db;
    private readonly IConfiguration _configuration;

    public FotoPerfilService(CasagresDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task<string> GuardarFotoAsync(
        long usuarioId, Stream contenido, long tamanoBytes, string? contentType)
    {
        if (contentType == null || !ExtensionesPorContentType.TryGetValue(contentType, out var extension))
        {
            throw new ArgumentException("Formato de imagen no soportado. Usa JPG, PNG o WEBP.");
        }

        if (tamanoBytes <= 0 || tamanoBytes > TamanoMaximoBytes)
        {
            throw new ArgumentException("La imagen no puede superar los 3 MB.");
        }

        var usuario = await _db.Usuarios.FindAsync(usuarioId)
            ?? throw new InvalidOperationException("Usuario no encontrado.");

        var carpeta = ObtenerCarpetaFotos();
        Directory.CreateDirectory(carpeta);

        BorrarFotoExistente(carpeta, usuarioId);

        var rutaDestino = Path.Combine(carpeta, $"{usuarioId}{extension}");

        await using (var archivoDestino = File.Create(rutaDestino))
        {
            await contenido.CopyToAsync(archivoDestino);
        }

        // El parámetro "v" evita que el navegador siga mostrando, desde su
        // caché, la foto anterior después de subir una nueva con esta misma
        // URL (que no cambia de nombre entre una subida y otra).
        var url = $"/api/auth/foto-perfil/{usuarioId}?v={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        usuario.FotoUrl = url;
        await _db.SaveChangesAsync();

        return url;
    }

    public (Stream Contenido, string ContentType)? ObtenerFoto(long usuarioId)
    {
        var carpeta = ObtenerCarpetaFotos();

        if (!Directory.Exists(carpeta))
        {
            return null;
        }

        var archivo = Directory
            .EnumerateFiles(carpeta, $"{usuarioId}.*")
            .FirstOrDefault();

        if (archivo == null)
        {
            return null;
        }

        var contentType = ExtensionesPorContentType
            .FirstOrDefault(par => par.Value == Path.GetExtension(archivo))
            .Key ?? "application/octet-stream";

        return (File.OpenRead(archivo), contentType);
    }

    private static void BorrarFotoExistente(string carpeta, long usuarioId)
    {
        foreach (var archivo in Directory.EnumerateFiles(carpeta, $"{usuarioId}.*"))
        {
            File.Delete(archivo);
        }
    }

    private string ObtenerCarpetaFotos()
    {
        var carpetaDatos = _configuration["Rutas:CarpetaDatos"];

        if (string.IsNullOrWhiteSpace(carpetaDatos))
        {
            throw new InvalidOperationException("No se encontró la configuración Rutas:CarpetaDatos.");
        }

        return Path.Combine(carpetaDatos, NombreCarpeta);
    }
}
