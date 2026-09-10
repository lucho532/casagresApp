using Casagres.API.Services.Pronostico;
using Casagres.API.Tests.TestHelpers;

namespace Casagres.API.Tests.Services.Pronostico;

public class HistoricoVentasServiceTests : IDisposable
{
    private readonly CarpetaDatosTemporal _carpeta = new();
    private readonly HistoricoVentasService _servicio;

    public HistoricoVentasServiceTests()
    {
        _servicio = new HistoricoVentasService(_carpeta.Rutas);
    }

    public void Dispose() => _carpeta.Dispose();

    [Fact]
    public void Leer_CuandoNoExisteElArchivo_LanzaFileNotFoundException()
    {
        var excepcion = Assert.Throws<FileNotFoundException>(() => _servicio.Leer(null));

        Assert.Equal("No se encontró series_mensuales.csv.", excepcion.Message);
    }

    [Fact]
    public void Leer_CuandoElArchivoNoTieneFilasDeDatos_LanzaDatosNoEncontradosException()
    {
        _carpeta.EscribirDatosPreprocesados("series_mensuales.csv", "mes,REF1,REF2");

        var excepcion = Assert.Throws<DatosNoEncontradosException>(() => _servicio.Leer(null));

        Assert.Equal("El archivo histórico no contiene datos.", excepcion.Message);
    }

    [Fact]
    public void Leer_SinReferencia_SumaTodasLasColumnasDeCadaMes()
    {
        _carpeta.EscribirDatosPreprocesados(
            "series_mensuales.csv",
            "mes,REF1,REF2",
            "2025-01-01,10,20",
            "2025-02-01,5,7");

        var resultado = _servicio.Leer(null);

        Assert.Null(resultado.Referencia);
        Assert.Equal(2, resultado.CantidadMeses);
        Assert.Equal(30, resultado.Historico[0].Cantidad);
        Assert.Equal(12, resultado.Historico[1].Cantidad);
    }

    [Fact]
    public void Leer_ConReferenciaValida_DevuelveSoloLaColumnaDeEsaReferencia()
    {
        _carpeta.EscribirDatosPreprocesados(
            "series_mensuales.csv",
            "mes,REF1,REF2",
            "2025-01-01,10,20");

        var resultado = _servicio.Leer("REF2");

        Assert.Equal("REF2", resultado.Referencia);
        var mes = Assert.Single(resultado.Historico);
        Assert.Equal("2025-01-01", mes.Mes);
        Assert.Equal(20, mes.Cantidad);
    }

    [Fact]
    public void Leer_LaBusquedaDeReferenciaEsInsensibleAMayusculas()
    {
        _carpeta.EscribirDatosPreprocesados(
            "series_mensuales.csv",
            "mes,REF1",
            "2025-01-01,10");

        var resultado = _servicio.Leer("ref1");

        var mes = Assert.Single(resultado.Historico);
        Assert.Equal(10, mes.Cantidad);
    }

    [Fact]
    public void Leer_ConReferenciaInexistente_LanzaDatosNoEncontradosException()
    {
        _carpeta.EscribirDatosPreprocesados(
            "series_mensuales.csv",
            "mes,REF1",
            "2025-01-01,10");

        var excepcion = Assert.Throws<DatosNoEncontradosException>(
            () => _servicio.Leer("NO-EXISTE"));

        Assert.Equal("No se encontró la referencia 'NO-EXISTE'.", excepcion.Message);
    }

    [Fact]
    public void Leer_ConReferencia_CuandoUnaFilaEsMasCortaQueSuColumna_DescartaEseMes()
    {
        // REF2 está en la columna 2, pero la fila de febrero solo trae la
        // columna 1 (le falta el dato de REF2 por completo).
        _carpeta.EscribirDatosPreprocesados(
            "series_mensuales.csv",
            "mes,REF1,REF2",
            "2025-01-01,10,20",
            "2025-02-01,5");

        var resultado = _servicio.Leer("REF2");

        var mes = Assert.Single(resultado.Historico);
        Assert.Equal("2025-01-01", mes.Mes);
        Assert.Equal(1, resultado.CantidadMeses);
    }

    [Fact]
    public void Leer_ConValorNoNumerico_LoTrataComoCero()
    {
        _carpeta.EscribirDatosPreprocesados(
            "series_mensuales.csv",
            "mes,REF1",
            "2025-01-01,no-es-un-numero");

        var resultado = _servicio.Leer("REF1");

        Assert.Equal(0, resultado.Historico.Single().Cantidad);
    }

    [Fact]
    public void Leer_IgnoraLineasEnBlanco()
    {
        _carpeta.EscribirDatosPreprocesados(
            "series_mensuales.csv",
            "mes,REF1",
            "",
            "2025-01-01,10",
            "   ");

        var resultado = _servicio.Leer("REF1");

        Assert.Equal(1, resultado.CantidadMeses);
    }
}
