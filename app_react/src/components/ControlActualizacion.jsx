import { useEffect, useRef, useState } from "react";

import { actualizarDatos, obtenerEstadoActualizacion } from "../services/api";

import "../styles/ControlActualizacion.css";

function ControlActualizacion({
  onUltimaActualizacion,
  onActualizacionCompletada,
}) {
  const [actualizando, setActualizando] = useState(false);
  const [estadoActualizacion, setEstadoActualizacion] = useState(null);
  const [horizonte, setHorizonte] = useState(1);

  const intervaloRef = useRef(null);

  // Indica que nosotros iniciamos una actualización
  const solicitudEnCursoRef = useRef(false);

  // Evita ejecutar varias veces el callback de finalización
  const actualizacionSolicitadaRef = useRef(false);

  const detenerSeguimiento = () => {
    if (intervaloRef.current) {
      clearInterval(intervaloRef.current);
      intervaloRef.current = null;
    }
  };

  const consultarEstado = async () => {
    try {
      const estado = await obtenerEstadoActualizacion();

      console.log("Estado del pipeline:", estado);

      setEstadoActualizacion(estado);

      if (estado.ultimaActualizacion) {
        onUltimaActualizacion?.(estado.ultimaActualizacion);
      }

      // El pipeline sigue ejecutándose
      if (estado.ejecutando) {
        setActualizando(true);
        return;
      }

      // Si la petición POST todavía no ha terminado,
      // esperamos antes de considerar finalizada la actualización.
      if (solicitudEnCursoRef.current) {
        return;
      }

      // El pipeline terminó
      setActualizando(false);
      detenerSeguimiento();

      // Avisamos al componente padre para que vuelva a cargar
      // los datos del dashboard.
      if (actualizacionSolicitadaRef.current) {
        actualizacionSolicitadaRef.current = false;

        console.log("Actualización completada. Recargando dashboard...");

        onActualizacionCompletada?.();
      }
    } catch (error) {
      console.error("Error consultando estado de actualización:", error);
    }
  };

  const iniciarSeguimiento = () => {
    if (intervaloRef.current) {
      return;
    }

    intervaloRef.current = setInterval(() => {
      consultarEstado();
    }, 1000);

    consultarEstado();
  };

  const ejecutarActualizacion = async () => {
    if (actualizando) {
      return;
    }

    console.log("Iniciando actualización...");

    solicitudEnCursoRef.current = true;
    actualizacionSolicitadaRef.current = true;

    setActualizando(true);

    setEstadoActualizacion({
      ejecutando: true,
      estado: "Iniciando actualización...",
      progreso: 0,
    });

    iniciarSeguimiento();

    try {
      await actualizarDatos(horizonte);

      console.log("Solicitud de actualización completada por el backend.");

      solicitudEnCursoRef.current = false;

      // El backend espera a que termine todo el pipeline antes
      // de responder. Por eso aquí los archivos ya deberían estar
      // actualizados.
      await consultarEstado();
    } catch (error) {
      console.error("Error iniciando actualización:", error);

      solicitudEnCursoRef.current = false;
      actualizacionSolicitadaRef.current = false;

      // Ya hay otra actualización ejecutándose
      if (error.response?.status === 409) {
        console.log("Ya existe una actualización en curso.");

        setActualizando(true);

        setEstadoActualizacion({
          ejecutando: true,
          estado: "Ya hay una actualización en curso.",
          progreso: 0,
        });

        // Seguimos consultando hasta que termine
        iniciarSeguimiento();

        return;
      }

      setActualizando(false);

      setEstadoActualizacion({
        ejecutando: false,
        estado:
          error.response?.data?.mensaje || "Error durante la actualización.",
        progreso: 0,
        error: error.response?.data?.detalle || error.message,
      });

      detenerSeguimiento();
    }
  };

  useEffect(() => {
    let componenteActivo = true;

    const comprobarActualizacion = async () => {
      try {
        const estado = await obtenerEstadoActualizacion();

        if (!componenteActivo) {
          return;
        }

        console.log("Estado inicial del pipeline:", estado);

        setEstadoActualizacion(estado);

        if (estado.ultimaActualizacion) {
          onUltimaActualizacion?.(estado.ultimaActualizacion);
        }

        // Si al entrar al dashboard ya había una actualización
        // ejecutándose, solamente seguimos su progreso.
        if (estado.ejecutando) {
          setActualizando(true);
          iniciarSeguimiento();
        }
      } catch (error) {
        console.error("Error obteniendo estado inicial:", error);
      }
    };

    comprobarActualizacion();

    return () => {
      componenteActivo = false;
      detenerSeguimiento();
    };
  }, []);

  return (
    <div className="control-actualizacion">
      <div className="selector-horizonte">
        <label htmlFor="horizonte">Horizonte:</label>

        <select
          id="horizonte"
          value={horizonte}
          onChange={(e) => setHorizonte(Number(e.target.value))}
          disabled={actualizando}
        >
          <option value={1}>1 mes</option>
          <option value={3}>3 meses</option>
          <option value={6}>6 meses</option>
          <option value={12}>12 meses</option>
        </select>
      </div>

      <button
        className={`boton-actualizar ${actualizando ? "actualizando" : ""}`}
        onClick={ejecutarActualizacion}
        disabled={actualizando}
      >
        <span className="icono-actualizar">{actualizando ? "↻" : "⟳"}</span>

        <span>{actualizando ? "Actualizando..." : "Actualizar datos"}</span>
      </button>

      {estadoActualizacion && (
        <div className="estado-actualizacion">
          <div className="estado-texto">
            <span
              className={`estado-punto ${
                estadoActualizacion.ejecutando
                  ? "estado-punto-activo"
                  : estadoActualizacion.error
                    ? "estado-punto-error"
                    : "estado-punto-ok"
              }`}
            />

            <span>{estadoActualizacion.estado}</span>
          </div>

          {estadoActualizacion.ejecutando && (
            <>
              <div className="barra-progreso">
                <div
                  className="barra-progreso-relleno"
                  style={{
                    width: `${Math.min(
                      estadoActualizacion.progreso || 0,
                      100,
                    )}%`,
                  }}
                />
              </div>

              <div className="progreso-porcentaje">
                {estadoActualizacion.progreso || 0}%
              </div>
            </>
          )}
        </div>
      )}
    </div>
  );
}

export default ControlActualizacion;
