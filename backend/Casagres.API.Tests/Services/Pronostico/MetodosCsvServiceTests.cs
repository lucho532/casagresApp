using Casagres.API.Services.Pronostico;
using Casagres.API.Tests.TestHelpers;

namespace Casagres.API.Tests.Services.Pronostico;

public class MetodosCsvServiceTests : IDisposable
{
    private readonly CarpetaDatosTemporal _carpeta = new();
    private readonly MetodosCsvService _servicio;

    private const string Encabezado = "codigo_producto,metodo,tiene_hiperparametros,n_scores_aci,alpha_aci";

    public MetodosCsvServiceTests()
    {
        _servicio = new MetodosCsvService(_carpeta.Rutas);
    }

    public void Dispose() => _carpeta.Dispose();

    [Fact]
    public void Leer_CuandoNoExisteElArchivo_LanzaFileNotFoundException()
    {
        var excepcion = Assert.Throws<FileNotFoundException>(() => _servicio.Leer());

        Assert.Equal("No se encontró el archivo de métodos.", excepcion.Message);
    }

    [Fact]
    public void Leer_CuandoSoloHayEncabezados_DevuelveRespuestaVacia()
    {
        _carpeta.EscribirSalida("metodo_por_serie.csv", Encabezado);

        var resultado = _servicio.Leer();

        Assert.Equal(0, resultado.CantidadProductos);
        Assert.Empty(resultado.Productos);
    }

    [Fact]
    public void Leer_ConUnaFilaValida_DevuelveElProductoCompleto()
    {
        _carpeta.EscribirSalida(
            "metodo_por_serie.csv",
            Encabezado,
            "REF1,KRR,true,12,0.1");

        var resultado = _servicio.Leer();

        var producto = Assert.Single(resultado.Productos);
        Assert.Equal("REF1", producto.Referencia);
        Assert.Equal("KRR", producto.Metodo);
        Assert.True(producto.TieneHiperparametros);
        Assert.Equal(12, producto.NScoresAci);
        Assert.Equal(0.1, producto.AlphaAci);
    }

    [Fact]
    public void Leer_ConReferenciaVacia_DescartaLaFila()
    {
        _carpeta.EscribirSalida(
            "metodo_por_serie.csv",
            Encabezado,
            " ,ANIO_ANTERIOR,false,0,0.1");

        var resultado = _servicio.Leer();

        Assert.Empty(resultado.Productos);
    }

    [Fact]
    public void Leer_ConBooleanoInvalido_DescartaLaFila()
    {
        _carpeta.EscribirSalida(
            "metodo_por_serie.csv",
            Encabezado,
            "REF1,KRR,no-es-booleano,12,0.1");

        var resultado = _servicio.Leer();

        Assert.Empty(resultado.Productos);
    }

    [Fact]
    public void Leer_ConMenosColumnasDeLasEsperadas_DescartaLaFila()
    {
        _carpeta.EscribirSalida(
            "metodo_por_serie.csv",
            Encabezado,
            "REF1,KRR,true");

        var resultado = _servicio.Leer();

        Assert.Empty(resultado.Productos);
    }
}
