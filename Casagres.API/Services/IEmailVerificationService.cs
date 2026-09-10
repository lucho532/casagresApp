using Casagres.API.Models;

namespace Casagres.API.Services;

public interface IEmailVerificationService
{
    Task EnviarCorreoDeVerificacionAsync(Usuario usuario);

    Task ReenviarSiNoVerificadoAsync(string email);

    Task<bool> VerificarAsync(string token);
}
