using Casagres.API.Services.Pronostico;

namespace Casagres.API.Tests.Services.Pronostico;

public class CsvCampoParserTests
{
    // ============================================================
    // ADouble
    // ============================================================

    [Theory]
    [InlineData("123.45", 123.45)]
    [InlineData("-10.5", -10.5)]
    [InlineData("0", 0.0)]
    [InlineData("0.0", 0.0)]
    // NumberStyles.Any admite separador de miles: la coma se descarta.
    [InlineData("1,234", 1234.0)]
    public void ADouble_ConTextoValido_DevuelveElNumero(string valor, double esperado)
    {
        var resultado = CsvCampoParser.ADouble(valor);

        Assert.Equal(esperado, resultado);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("1.2.3")]
    [InlineData("N/A")]
    public void ADouble_ConTextoInvalido_DevuelveNull(string valor)
    {
        var resultado = CsvCampoParser.ADouble(valor);

        Assert.Null(resultado);
    }

    // ============================================================
    // AEntero
    // ============================================================

    [Theory]
    [InlineData("42", 42)]
    [InlineData("-5", -5)]
    [InlineData("0", 0)]
    public void AEntero_ConTextoValido_DevuelveElNumero(string valor, int esperado)
    {
        var resultado = CsvCampoParser.AEntero(valor);

        Assert.Equal(esperado, resultado);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("3.14")]
    public void AEntero_ConTextoInvalido_DevuelveNull(string valor)
    {
        var resultado = CsvCampoParser.AEntero(valor);

        Assert.Null(resultado);
    }

    // ============================================================
    // ABooleano
    // ============================================================

    [Theory]
    [InlineData("true", true)]
    [InlineData("True", true)]
    [InlineData("TRUE", true)]
    [InlineData(" true ", true)]
    [InlineData("false", false)]
    [InlineData(" False ", false)]
    public void ABooleano_ConTextoValido_DevuelveElBooleano(string valor, bool esperado)
    {
        var resultado = CsvCampoParser.ABooleano(valor);

        Assert.Equal(esperado, resultado);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("0")]
    [InlineData("si")]
    public void ABooleano_ConTextoInvalido_DevuelveNull(string valor)
    {
        var resultado = CsvCampoParser.ABooleano(valor);

        Assert.Null(resultado);
    }
}
