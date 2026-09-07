namespace Casagres.API.Services;

public class PipelineEstadoService
{
    private readonly object _lock = new();

    public bool Ejecutando { get; private set; }

    public string Estado { get; private set; } =
        "Sin actualizaciones en curso.";

    public int Progreso { get; private set; }

    public DateTime? Inicio { get; private set; }

    public DateTime? Fin { get; private set; }

    public DateTime? UltimaActualizacion { get; private set; }

    public string? Error { get; private set; }

    public void Iniciar()
    {
        lock (_lock)
        {
            Ejecutando = true;
            Estado = "Iniciando actualización...";
            Progreso = 0;
            Inicio = DateTime.Now;
            Error = null;
        }
    }

    public void Actualizar(
        string estado,
        int progreso)
    {
        lock (_lock)
        {
            Estado = estado;
            Progreso = progreso;
        }
    }

    public void Completar()
    {
        lock (_lock)
        {
            Ejecutando = false;
            Estado = "Actualización completada.";
            Progreso = 100;

            Fin = DateTime.Now;

            UltimaActualizacion = Fin;
        }
    }

    public void Fallar(string error)
    {
        lock (_lock)
        {
            Ejecutando = false;
            Estado = "Error durante la actualización.";

            Error = error;

            Fin = DateTime.Now;
        }
    }
}