namespace Casagres.API;

public interface IMicrosoftAuthService
{
    Task<string> ObtenerTokenAsync();
}
