using Casagres.API.Services.Pronostico;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Casagres.API.Tests.TestHelpers;

/// <summary>
/// Carpeta temporal real en disco usada por los tests de los servicios que
/// leen archivos de "Rutas:CarpetaDatos". Se crea una carpeta única por
/// prueba y se borra al terminar (IDisposable).
/// </summary>
public sealed class CarpetaDatosTemporal : IDisposable
{
    public string Ruta { get; }

    public RutasDatosService Rutas { get; }

    public CarpetaDatosTemporal()
    {
        Ruta = Path.Combine(Path.GetTempPath(), "casagres-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(Ruta);

        var configuracion = new Mock<IConfiguration>();
        configuracion.Setup(c => c["Rutas:CarpetaDatos"]).Returns(Ruta);

        Rutas = new RutasDatosService(configuracion.Object);
    }

    public string EscribirSalida(string nombreArchivo, params string[] lineas) =>
        Escribir(Path.Combine(Ruta, "salidas_prediccion"), nombreArchivo, lineas);

    public string EscribirDatosPreprocesados(string nombreArchivo, params string[] lineas) =>
        Escribir(Path.Combine(Ruta, "datos_preprocesados"), nombreArchivo, lineas);

    private static string Escribir(string carpeta, string nombreArchivo, string[] lineas)
    {
        Directory.CreateDirectory(carpeta);

        var ruta = Path.Combine(carpeta, nombreArchivo);
        File.WriteAllLines(ruta, lineas);

        return ruta;
    }

    public void Dispose()
    {
        if (Directory.Exists(Ruta))
        {
            Directory.Delete(Ruta, recursive: true);
        }
    }
}
