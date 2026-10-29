namespace Casagres.API.Services;

public interface IFotoPerfilService
{
    Task<string> GuardarFotoAsync(long usuarioId, Stream contenido, long tamanoBytes, string? contentType);

    (Stream Contenido, string ContentType)? ObtenerFoto(long usuarioId);
}
