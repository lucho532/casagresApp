using System.Text.Json;
using Casagres.API.Middleware;
using Casagres.API.Services.Pronostico;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Casagres.API.Tests.Middleware;

public class ManejadorGlobalDeExcepcionesTests
{
    private static async Task<(int Estado, string Mensaje)> ManejarAsync(Exception excepcion)
    {
        var contexto = new DefaultHttpContext();
        contexto.Response.Body = new MemoryStream();

        var manejador = new ManejadorGlobalDeExcepciones(
            NullLogger<ManejadorGlobalDeExcepciones>.Instance);

        var manejada = await manejador.TryHandleAsync(contexto, excepcion, CancellationToken.None);

        Assert.True(manejada);

        contexto.Response.Body.Position = 0;
        var json = await JsonDocument.ParseAsync(contexto.Response.Body);

        return (contexto.Response.StatusCode, json.RootElement.GetProperty("mensaje").GetString()!);
    }

    [Fact]
    public async Task ArchivoNoEncontrado_Devuelve404ConElMensaje()
    {
        var (estado, mensaje) = await ManejarAsync(new FileNotFoundException("No se encontró pronostico.csv."));

        Assert.Equal(404, estado);
        Assert.Equal("No se encontró pronostico.csv.", mensaje);
    }

    [Fact]
    public async Task ArchivoNoEncontrado_NoExponeLaRutaDelServidor()
    {
        var (_, mensaje) = await ManejarAsync(new FileNotFoundException("falta", @"C:\datos\secreto.csv"));

        Assert.DoesNotContain("secreto.csv", mensaje);
    }

    [Fact]
    public async Task DatosNoEncontrados_Devuelve404ConElMensaje()
    {
        var (estado, mensaje) = await ManejarAsync(new DatosNoEncontradosException("sin datos"));

        Assert.Equal(404, estado);
        Assert.Equal("sin datos", mensaje);
    }

    [Theory]
    [InlineData(typeof(InvalidOperationException))]
    [InlineData(typeof(Exception))]
    public async Task OtraExcepcion_Devuelve500GenericoSinExponerElMensajeInterno(Type tipo)
    {
        var excepcion = (Exception)Activator.CreateInstance(tipo, "detalle interno secreto")!;

        var (estado, mensaje) = await ManejarAsync(excepcion);

        Assert.Equal(500, estado);
        Assert.DoesNotContain("secreto", mensaje);
    }
}
