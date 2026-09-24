using Casagres.API.Services.Pronostico;
using Casagres.API.Tests.TestHelpers;

namespace Casagres.API.Tests.Services.Pronostico;

public class DashboardPronosticoServiceTests : IDisposable
{
    private readonly CarpetaDatosTemporal _carpeta = new();
    private readonly DashboardPronosticoService _servicio;

    private const string EncabezadoIntervalos =
        "mes,referencia,metodo,prediccion,inferior,superior,half_width,alpha_aci";

    private const string EncabezadoMetodos =
        "codigo_producto,metodo,tiene_hiperparametros,n_scores_aci,alpha_aci";

    public DashboardPronosticoServiceTests()
    {
        _servicio = new DashboardPronosticoService(_carpeta.Rutas);
    }

    public void Dispose() => _carpeta.Dispose();

    private void EscribirTresArchivosMinimos()
    {
        _carpeta.EscribirSalida("pronostico.csv", "mes,REF1", "2025-01-01,80");
        _carpeta.EscribirSalida("pronostico_intervalos.csv", EncabezadoIntervalos);
        _carpeta.EscribirSalida("metodo_por_serie.csv", EncabezadoMetodos);
    }

    [Fact]
    public void Leer_CuandoFaltaPronosticoCsv_LanzaFileNotFoundException()
    {
        var excepcion = Assert.Throws<FileNotFoundException>(() => _servicio.Leer());

        Assert.Equal("No se encontró pronostico.csv.", excepcion.Message);
    }

    [Fact]
    public void Leer_CuandoFaltaIntervalosCsv_LanzaFileNotFoundException()
    {
        _carpeta.EscribirSalida("pronostico.csv", "mes,REF1", "2025-01-01,80");
        _carpeta.EscribirSalida("metodo_por_serie.csv", EncabezadoMetodos);

        var excepcion = Assert.Throws<FileNotFoundException>(() => _servicio.Leer());

        Assert.Equal("No se encontró pronostico_intervalos.csv.", excepcion.Message);
    }

    [Fact]
    public void Leer_CuandoFaltaMetodosCsv_LanzaFileNotFoundException()
    {
        _carpeta.EscribirSalida("pronostico.csv", "mes,REF1", "2025-01-01,80");
        _carpeta.EscribirSalida("pronostico_intervalos.csv", EncabezadoIntervalos);

        var excepcion = Assert.Throws<FileNotFoundException>(() => _servicio.Leer());

        Assert.Equal("No se encontró metodo_por_serie.csv.", excepcion.Message);
    }

    [Fact]
    public void Leer_CuandoPronosticoNoTieneFilasDeDatos_LanzaDatosNoEncontradosException()
    {
        _carpeta.EscribirSalida("pronostico.csv", "mes,REF1");
        _carpeta.EscribirSalida("pronostico_intervalos.csv", EncabezadoIntervalos);
        _carpeta.EscribirSalida("metodo_por_serie.csv", EncabezadoMetodos);

        var excepcion = Assert.Throws<DatosNoEncontradosException>(() => _servicio.Leer());

        Assert.Equal("pronostico.csv no contiene datos.", excepcion.Message);
    }

    [Fact]
    public void Leer_ConDatosCompletos_CombinaPronosticoIntervaloYMetodoPorMesYReferencia()
    {
        _carpeta.EscribirSalida("pronostico.csv", "mes,REF1", "2025-01-01,80");
        _carpeta.EscribirSalida(
            "pronostico_intervalos.csv",
            EncabezadoIntervalos,
            "2025-01-01,REF1,KRR,80,60,100,20,0.1");
        _carpeta.EscribirSalida(
            "metodo_por_serie.csv",
            EncabezadoMetodos,
            "REF1,KRR,true,15,0.1");

        var resultado = _servicio.Leer();

        var mes = Assert.Single(resultado.Meses);
        Assert.Equal("2025-01-01", mes.Mes);

        var producto = Assert.Single(mes.Productos);
        Assert.Equal("REF1", producto.Referencia);
        Assert.Equal(80, producto.Pronostico);
        Assert.Equal("KRR", producto.Metodo);
        Assert.True(producto.TieneHiperparametros);
        Assert.Equal(15, producto.NScoresAci);
        Assert.Equal(60, producto.Inferior);
        Assert.Equal(100, producto.Superior);
        Assert.Equal(20, producto.HalfWidth);
        Assert.Equal(0.1, producto.AlphaAci);
    }

    [Fact]
    public void Leer_OrdenaLosMesesCronologicamenteSinImportarElOrdenDelArchivo()
    {
        _carpeta.EscribirSalida(
            "pronostico.csv",
            "mes,REF1",
            "2025-03-01,10",
            "2025-01-01,20",
            "2025-02-01,30");
        _carpeta.EscribirSalida("pronostico_intervalos.csv", EncabezadoIntervalos);
        _carpeta.EscribirSalida("metodo_por_serie.csv", EncabezadoMetodos);

        var resultado = _servicio.Leer();

        Assert.Equal(
            new[] { "2025-01-01", "2025-02-01", "2025-03-01" },
            resultado.Meses.Select(m => m.Mes));
    }

    [Fact]
    public void Leer_CuandoNoHayMetodoRegistradoParaLaReferencia_UsaValoresPorDefecto()
    {
        _carpeta.EscribirSalida("pronostico.csv", "mes,REF-SIN-METODO", "2025-01-01,80");
        _carpeta.EscribirSalida("pronostico_intervalos.csv", EncabezadoIntervalos);
        _carpeta.EscribirSalida("metodo_por_serie.csv", EncabezadoMetodos);

        var resultado = _servicio.Leer();

        var producto = Assert.Single(resultado.Meses.Single().Productos);
        Assert.Null(producto.Metodo);
        Assert.False(producto.TieneHiperparametros);
        Assert.Equal(0, producto.NScoresAci);
    }

    [Fact]
    public void Leer_CuandoNoHayIntervaloParaEseMesYReferencia_UsaCeroEnElIntervalo()
    {
        _carpeta.EscribirSalida("pronostico.csv", "mes,REF1", "2025-01-01,80");
        _carpeta.EscribirSalida("pronostico_intervalos.csv", EncabezadoIntervalos);
        _carpeta.EscribirSalida("metodo_por_serie.csv", EncabezadoMetodos);

        var resultado = _servicio.Leer();

        var producto = Assert.Single(resultado.Meses.Single().Productos);
        Assert.Equal(0, producto.Inferior);
        Assert.Equal(0, producto.Superior);
        Assert.Equal(0, producto.HalfWidth);
    }

    [Fact]
    public void Leer_ElIntervaloEsespecificoPorMesAunqueLaReferenciaSeaLaMisma()
    {
        // La misma referencia REF1 tiene un pronóstico en dos meses distintos,
        // y cada mes debe tomar su propio intervalo (no el de otro mes).
        _carpeta.EscribirSalida(
            "pronostico.csv",
            "mes,REF1",
            "2025-01-01,80",
            "2025-02-01,90");
        _carpeta.EscribirSalida(
            "pronostico_intervalos.csv",
            EncabezadoIntervalos,
            "2025-01-01,REF1,KRR,80,60,100,20,0.1",
            "2025-02-01,REF1,KRR,90,70,110,20,0.2");
        _carpeta.EscribirSalida("metodo_por_serie.csv", EncabezadoMetodos, "REF1,KRR,true,15,0.1");

        var resultado = _servicio.Leer();

        var productoEnero = resultado.Meses.Single(m => m.Mes == "2025-01-01").Productos.Single();
        var productoFebrero = resultado.Meses.Single(m => m.Mes == "2025-02-01").Productos.Single();

        Assert.Equal(60, productoEnero.Inferior);
        Assert.Equal(70, productoFebrero.Inferior);
    }

    [Fact]
    public void Leer_SinPronosticoHistoricoCsv_MesesHistoricosQuedaVacio()
    {
        EscribirTresArchivosMinimos();

        var resultado = _servicio.Leer();

        Assert.Single(resultado.Meses);
        Assert.Empty(resultado.MesesHistoricos);
    }

    [Fact]
    public void Leer_ConPronosticoHistoricoCsv_LoExponeSeparadoDeLosMesesFuturos()
    {
        // El histórico va en su propio campo, NO mezclado con Meses: el
        // resto de la app (selector de horizonte en Inicio/Demanda futura/
        // Decisiones/Tendencias) asume que Meses solo trae pronóstico a
        // futuro y toma su primer elemento como el horizonte por defecto.
        EscribirTresArchivosMinimos();
        _carpeta.EscribirSalida(
            "pronostico_historico.csv",
            "mes,codigo_producto,metodo,prediccion,inferior,superior",
            "2024-11-01,REF1,KRR,70,60,80",
            "2024-12-01,REF1,KRR,75,65,85");

        var resultado = _servicio.Leer();

        Assert.Equal(new[] { "2025-01-01" }, resultado.Meses.Select(m => m.Mes));
        Assert.Equal(
            new[] { "2024-11-01", "2024-12-01" },
            resultado.MesesHistoricos.Select(m => m.Mes));

        var productoNoviembre = resultado.MesesHistoricos
            .Single(m => m.Mes == "2024-11-01").Productos.Single();
        Assert.Equal("REF1", productoNoviembre.Referencia);
        Assert.Equal(70, productoNoviembre.Pronostico);
        Assert.Equal("KRR", productoNoviembre.Metodo);
        Assert.Equal(60, productoNoviembre.Inferior);
        Assert.Equal(80, productoNoviembre.Superior);
    }

    [Fact]
    public void Leer_ConVariosProductosEnElMismoMesHistorico_LosAgrupaEnUnSoloMes()
    {
        EscribirTresArchivosMinimos();
        _carpeta.EscribirSalida(
            "pronostico_historico.csv",
            "mes,codigo_producto,metodo,prediccion,inferior,superior",
            "2024-11-01,REF1,KRR,70,60,80",
            "2024-11-01,REF2,ANIO_ANTERIOR,20,15,25");

        var resultado = _servicio.Leer();

        var mesNoviembre = resultado.MesesHistoricos.Single(m => m.Mes == "2024-11-01");
        Assert.Equal(2, mesNoviembre.Productos.Count);
        Assert.Contains(mesNoviembre.Productos, p => p.Referencia == "REF1");
        Assert.Contains(mesNoviembre.Productos, p => p.Referencia == "REF2");
    }
}
