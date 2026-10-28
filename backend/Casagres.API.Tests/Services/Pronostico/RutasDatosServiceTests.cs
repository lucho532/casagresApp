using Casagres.API.Services.Pronostico;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Casagres.API.Tests.Services.Pronostico;

public class RutasDatosServiceTests
{
    private static RutasDatosService CrearServicio(string? carpetaDatos)
    {
        var configuracion = new Mock<IConfiguration>();
        configuracion.Setup(c => c["Rutas:CarpetaDatos"]).Returns(carpetaDatos);

        return new RutasDatosService(configuracion.Object);
    }

    [Fact]
    public void ObtenerCarpetaDatos_ConConfiguracionValida_DevuelveLaRuta()
    {
        var servicio = CrearServicio(@"C:\datos");

        var resultado = servicio.ObtenerCarpetaDatos();

        Assert.Equal(@"C:\datos", resultado);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ObtenerCarpetaDatos_SinConfiguracion_LanzaInvalidOperationException(string? carpetaDatos)
    {
        var servicio = CrearServicio(carpetaDatos);

        var excepcion = Assert.Throws<InvalidOperationException>(servicio.ObtenerCarpetaDatos);

        Assert.Equal("No está configurada la ruta de datos.", excepcion.Message);
    }

    [Fact]
    public void RutaSalidas_CombinaCarpetaDatosConSalidasPrediccion()
    {
        var servicio = CrearServicio(@"C:\datos");

        var resultado = servicio.RutaSalidas("pronostico.csv");

        Assert.Equal(
            Path.Combine(@"C:\datos", "salidas_prediccion", "pronostico.csv"),
            resultado);
    }

    [Fact]
    public void RutaDatosPreprocesados_CombinaCarpetaDatosConDatosPreprocesados()
    {
        var servicio = CrearServicio(@"C:\datos");

        var resultado = servicio.RutaDatosPreprocesados("series_mensuales.csv");

        Assert.Equal(
            Path.Combine(@"C:\datos", "datos_preprocesados", "series_mensuales.csv"),
            resultado);
    }

    [Fact]
    public void RutaSalidas_SinConfiguracion_PropagaLaExcepcion()
    {
        var servicio = CrearServicio(null);

        Assert.Throws<InvalidOperationException>(() => servicio.RutaSalidas("pronostico.csv"));
    }
}
