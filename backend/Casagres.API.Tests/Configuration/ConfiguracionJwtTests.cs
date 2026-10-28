using Casagres.API.Configuration;
using Microsoft.Extensions.Configuration;

namespace Casagres.API.Tests.Configuration;

public class ConfiguracionJwtTests
{
    private static IConfiguration Crear(Dictionary<string, string?> valores) =>
        new ConfigurationBuilder().AddInMemoryCollection(valores).Build();

    [Fact]
    public void SinConfigurar_UsaEmisorYAudienciaPorDefecto()
    {
        var config = Crear(new());

        Assert.Equal(ConfiguracionJwt.EmisorPorDefecto, ConfiguracionJwt.ObtenerEmisor(config));
        Assert.Equal(ConfiguracionJwt.AudienciaPorDefecto, ConfiguracionJwt.ObtenerAudiencia(config));
    }

    [Fact]
    public void Configurado_UsaLosValoresDeConfiguracion()
    {
        var config = Crear(new() { ["Jwt:Issuer"] = "mi-emisor", ["Jwt:Audience"] = "mi-audiencia" });

        Assert.Equal("mi-emisor", ConfiguracionJwt.ObtenerEmisor(config));
        Assert.Equal("mi-audiencia", ConfiguracionJwt.ObtenerAudiencia(config));
    }

    [Fact]
    public void ClaveObligatoria_SinClave_LanzaInvalidOperationException()
    {
        Assert.Throws<InvalidOperationException>(
            () => ConfiguracionJwt.ObtenerClaveObligatoria(Crear(new())));
    }

    [Fact]
    public void ClaveObligatoria_ConClave_LaDevuelve()
    {
        var config = Crear(new() { ["Jwt:Key"] = "una-clave" });

        Assert.Equal("una-clave", ConfiguracionJwt.ObtenerClaveObligatoria(config));
    }
}
