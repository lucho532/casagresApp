namespace Casagres.API.Services;

public interface IPasswordResetService
{
    Task SolicitarResetAsync(string email);

    Task<bool> RestablecerAsync(string token, string nuevaPassword);
}
