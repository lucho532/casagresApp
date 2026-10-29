namespace Casagres.API.Services.Pronostico;

public class RutasDatosService
{
    private readonly IConfiguration _configuration;

    public RutasDatosService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string ObtenerCarpetaDatos()
    {
        var carpetaDatos = _configuration["Rutas:CarpetaDatos"];

        if (string.IsNullOrWhiteSpace(carpetaDatos))
        {
            throw new InvalidOperationException(
                "No está configurada la ruta de datos.");
        }

        return carpetaDatos;
    }

    public string RutaSalidas(string archivo) =>
        Path.Combine(ObtenerCarpetaDatos(), "salidas_prediccion", archivo);

    public string RutaDatosPreprocesados(string archivo) =>
        Path.Combine(ObtenerCarpetaDatos(), "datos_preprocesados", archivo);
}
