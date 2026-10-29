using Casagres.API.Services.Pronostico;
using Casagres.API.Tests.TestHelpers;

namespace Casagres.API.Tests.Services.Pronostico;

public class PronosticoIntervalosCsvServiceTests : IDisposable
{
    private readonly CarpetaDatosTemporal _carpeta = new();
    private readonly PronosticoIntervalosCsvService _servicio;

    public PronosticoIntervalosCsvServiceTests()
    {
        _servicio = new PronosticoIntervalosCsvService(_carpeta.Rutas);
    }

    public void Dispose() => _carpeta.Dispose();

    private const string Encabezado = "mes,referencia,metodo,prediccion,inferior,superior,half_width,alpha_aci";

    [Fact]
    public void Leer_CuandoNoExisteElArchivo_LanzaFileNotFoundException()
    {
        var excepcion = Assert.Throws<FileNotFoundException>(() => _servicio.Leer());

        Assert.Equal("No se encontró el archivo de intervalos.", excepcion.Message);
    }

    [Fact]
    public void Leer_CuandoSoloHayEncabezados_DevuelveRespuestaVacia()
    {
        _carpeta.EscribirSalida("pronostico_intervalos.csv", Encabezado);

        var resultado = _servicio.Leer();

        Assert.Null(resultado.Mes);
        Assert.Equal(0, resultado.CantidadProductos);
    }

    [Fact]
    public void Leer_ConUnaFilaValida_DevuelveElProductoCompleto()
    {
        _carpeta.EscribirSalida(
            "pronostico_intervalos.csv",
            Encabezado,
            "2025-06-01,REF1,KRR,120.5,100,140,20,0.1");

        var resultado = _servicio.Leer();

        Assert.Equal("2025-06-01", resultado.Mes);
        Assert.Equal(1, resultado.CantidadProductos);

        var producto = Assert.Single(resultado.Productos);
        Assert.Equal("REF1", producto.Referencia);
        Assert.Equal("KRR", producto.Metodo);
        Assert.Equal(120.5, producto.Prediccion);
        Assert.Equal(100, producto.Inferior);
        Assert.Equal(140, producto.Superior);
        Assert.Equal(20, producto.HalfWidth);
        Assert.Equal(0.1, producto.AlphaAci);
    }

    [Fact]
    public void Leer_TomaElMesDeLaPrimeraLineaDeDatosAunqueEsaFilaSeaInvalida()
    {
        _carpeta.EscribirSalida(
            "pronostico_intervalos.csv",
            Encabezado,
            "2025-06-01,REF1,KRR,120.5,100,140", // fila inválida: faltan columnas
            "2025-07-01,REF2,KRR,80,60,100,20,0.1"); // fila válida

        var resultado = _servicio.Leer();

        // El mes se toma siempre de la primera línea de datos (lineas[1]),
        // sin importar si esa fila en particular produjo un producto válido.
        Assert.Equal("2025-06-01", resultado.Mes);
        Assert.Equal(1, resultado.CantidadProductos);
    }

    [Fact]
    public void Leer_DescartaFilasConMenosColumnasDeLasEsperadas()
    {
        _carpeta.EscribirSalida(
            "pronostico_intervalos.csv",
            Encabezado,
            "2025-06-01,REF1,KRR,120.5,100,140");

        var resultado = _servicio.Leer();

        Assert.Empty(resultado.Productos);
        // Sin productos válidos, el mes de la respuesta queda en null.
        Assert.Null(resultado.Mes);
    }

    [Fact]
    public void Leer_DescartaFilasConValoresNumericosInvalidos()
    {
        _carpeta.EscribirSalida(
            "pronostico_intervalos.csv",
            Encabezado,
            "2025-06-01,REF1,KRR,no-numero,100,140,20,0.1");

        var resultado = _servicio.Leer();

        Assert.Empty(resultado.Productos);
    }

    [Fact]
    public void Leer_IgnoraLineasEnBlanco()
    {
        _carpeta.EscribirSalida(
            "pronostico_intervalos.csv",
            Encabezado,
            "",
            "2025-06-01,REF1,KRR,120.5,100,140,20,0.1",
            "   ");

        var resultado = _servicio.Leer();

        Assert.Equal(1, resultado.CantidadProductos);
    }
}
