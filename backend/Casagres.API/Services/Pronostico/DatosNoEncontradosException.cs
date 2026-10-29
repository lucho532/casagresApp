namespace Casagres.API.Services.Pronostico;

/// <summary>
/// El archivo de datos existe pero no contiene la información solicitada
/// (por ejemplo, un archivo vacío o una referencia inexistente).
/// </summary>
public class DatosNoEncontradosException : Exception
{
    public DatosNoEncontradosException(string mensaje) : base(mensaje)
    {
    }
}
