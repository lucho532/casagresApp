using Casagres.API.Services;

namespace Casagres.API.Tests.Services;

public class ActualizacionEstadoServiceTests
{
    [Fact]
    public void EstadoInicial_NoEstaEjecutandoYNoTieneFechas()
    {
        var servicio = new ActualizacionEstadoService();

        Assert.False(servicio.Ejecutando);
        Assert.Equal("Sin actualizaciones en curso.", servicio.Estado);
        Assert.Equal(0, servicio.Progreso);
        Assert.Null(servicio.Inicio);
        Assert.Null(servicio.Fin);
        Assert.Null(servicio.UltimaActualizacion);
        Assert.Null(servicio.Error);
    }

    [Fact]
    public void Iniciar_MarcaEjecutandoYReiniciaProgresoYError()
    {
        var servicio = new ActualizacionEstadoService();
        servicio.Fallar("error previo");

        servicio.Iniciar();

        Assert.True(servicio.Ejecutando);
        Assert.Equal("Iniciando actualización...", servicio.Estado);
        Assert.Equal(0, servicio.Progreso);
        Assert.NotNull(servicio.Inicio);
        Assert.Null(servicio.Error);
    }

    [Fact]
    public void Actualizar_SoloCambiaEstadoYProgreso()
    {
        var servicio = new ActualizacionEstadoService();
        servicio.Iniciar();
        var inicioOriginal = servicio.Inicio;

        servicio.Actualizar("Procesando datos...", 45);

        Assert.Equal("Procesando datos...", servicio.Estado);
        Assert.Equal(45, servicio.Progreso);
        Assert.True(servicio.Ejecutando);
        Assert.Equal(inicioOriginal, servicio.Inicio);
    }

    [Fact]
    public void Completar_MarcaFinalizadoConProgreso100YRegistraUltimaActualizacion()
    {
        var servicio = new ActualizacionEstadoService();
        servicio.Iniciar();

        servicio.Completar();

        Assert.False(servicio.Ejecutando);
        Assert.Equal("Actualización completada.", servicio.Estado);
        Assert.Equal(100, servicio.Progreso);
        Assert.NotNull(servicio.Fin);
        Assert.Equal(servicio.Fin, servicio.UltimaActualizacion);
    }

    [Fact]
    public void Fallar_MarcaErrorSinTocarElProgresoAlcanzado()
    {
        var servicio = new ActualizacionEstadoService();
        servicio.Iniciar();
        servicio.Actualizar("Procesando...", 60);

        servicio.Fallar("Ocurrió un problema.");

        Assert.False(servicio.Ejecutando);
        Assert.Equal("Error durante la actualización.", servicio.Estado);
        Assert.Equal("Ocurrió un problema.", servicio.Error);
        Assert.Equal(60, servicio.Progreso);
        Assert.NotNull(servicio.Fin);
        Assert.Null(servicio.UltimaActualizacion);
    }
}
