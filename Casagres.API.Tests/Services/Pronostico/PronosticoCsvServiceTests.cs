using Casagres.API.Services.Pronostico;
using Casagres.API.Tests.TestHelpers;

namespace Casagres.API.Tests.Services.Pronostico;

public class PronosticoCsvServiceTests : IDisposable
{
    private readonly CarpetaDatosTemporal _carpeta = new();
    private readonly PronosticoCsvService _servicio;

    public PronosticoCsvServiceTests()
    {
        _servicio = new PronosticoCsvService(_carpeta.Rutas);
    }

    public void Dispose() => _carpeta.Dispose();

    [Fact]
    public void Leer_CuandoNoExisteElArchivo_LanzaFileNotFoundException()
    {
        var excepcion = Assert.Throws<FileNotFoundException>(() => _servicio.Leer());

        Assert.Equal("No se encontró el archivo de pronóstico.", excepcion.Message);
    }

    [Fact]
    public void Leer_CuandoSoloHayEncabezados_DevuelveRespuestaVacia()
    {
        _carpeta.EscribirSalida("pronostico.csv", "mes,REF1,REF2");

        var resultado = _servicio.Leer();

        Assert.Null(resultado.Mes);
        Assert.Equal(0, resultado.CantidadProductos);
        Assert.Empty(resultado.Productos);
    }

    [Fact]
    public void Leer_ConDatosValidos_DevuelveElMesYLosProductosConPronosticoDistintoDeCero()
    {
        _carpeta.EscribirSalida(
            "pronostico.csv",
            "mes,REF1,REF2,REF3",
            "2025-06-01,120.5,0,abc");

        var resultado = _servicio.Leer();

        Assert.Equal("2025-06-01", resultado.Mes);
        Assert.Equal(1, resultado.CantidadProductos);

        var producto = Assert.Single(resultado.Productos);
        Assert.Equal("REF1", producto.Referencia);
        Assert.Equal(120.5, producto.Pronostico);
    }

    [Fact]
    public void Leer_ExcluyeColumnasConPronosticoEnCero()
    {
        _carpeta.EscribirSalida(
            "pronostico.csv",
            "mes,REF1",
            "2025-06-01,0");

        var resultado = _servicio.Leer();

        Assert.Empty(resultado.Productos);
    }

    [Fact]
    public void Leer_ExcluyeColumnasConEncabezadoVacio()
    {
        _carpeta.EscribirSalida(
            "pronostico.csv",
            "mes,,REF2",
            "2025-06-01,50,80");

        var resultado = _servicio.Leer();

        var producto = Assert.Single(resultado.Productos);
        Assert.Equal("REF2", producto.Referencia);
    }

    [Fact]
    public void Leer_ExcluyeColumnasConValorNoNumerico()
    {
        _carpeta.EscribirSalida(
            "pronostico.csv",
            "mes,REF1",
            "2025-06-01,no-es-un-numero");

        var resultado = _servicio.Leer();

        Assert.Empty(resultado.Productos);
    }
}
