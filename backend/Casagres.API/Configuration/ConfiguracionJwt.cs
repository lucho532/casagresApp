namespace Casagres.API.Configuration;

// Valores de emisor y audiencia del JWT. Se leen de "Jwt:Issuer" y
// "Jwt:Audience"; si no están configurados se usan valores por defecto,
// de modo que la API funciona sin variables nuevas en el hosting.
public static class ConfiguracionJwt
{
    public const string EmisorPorDefecto = "Casagres.API";
    public const string AudienciaPorDefecto = "Casagres.Web";

    public static string ObtenerEmisor(IConfiguration configuracion) =>
        ValorOPorDefecto(configuracion["Jwt:Issuer"], EmisorPorDefecto);

    public static string ObtenerAudiencia(IConfiguration configuracion) =>
        ValorOPorDefecto(configuracion["Jwt:Audience"], AudienciaPorDefecto);

    // Falla al arrancar, con un mensaje claro, si falta la clave de firma.
    public static string ObtenerClaveObligatoria(IConfiguration configuracion)
    {
        var clave = configuracion["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(clave))
        {
            throw new InvalidOperationException(
                "No está configurada la clave JWT (Jwt:Key / variable Jwt__Key).");
        }

        return clave;
    }

    private static string ValorOPorDefecto(string? valor, string porDefecto) =>
        string.IsNullOrWhiteSpace(valor) ? porDefecto : valor;
}
