using Casagres.API.Models;

namespace Casagres.API.Services;

public interface IAuthService
{
    Task<Usuario?> ObtenerPorIdAsync(long id);

    Task<string?> LoginAsync(string usuario, string password);

    Task<bool> RegistrarAsync(string usuario, string password, string? nombre, string? email);

    Task<string?> LoginConMicrosoftAsync(string idToken);

    Task<string?> LoginConGoogleAsync(string accessToken);
}
