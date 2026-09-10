namespace Casagres.API.Services;

public class EmailNoVerificadoException : Exception
{
    public string? Email { get; }

    public EmailNoVerificadoException(string? email)
        : base("El correo electrónico no ha sido verificado.")
    {
        Email = email;
    }
}
