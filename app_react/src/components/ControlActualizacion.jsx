import { useCallback, useEffect, useRef, useState } from "react";

import { actualizarDatos, obtenerEstadoActualizacion } from "../services/api";

import "../styles/ControlActualizacion.css";

function ControlActualizacion({
  onUltimaActualizacion,
  onActualizacionCompletada,
}) {
  const HORIZONTE_MINIMO = 1;
  const HORIZONTE_MAXIMO = 24;

  const [actualizando, setActualizando] = useState(false);
  const [estadoActualizacion, setEstadoActualizacion] = useState(null);
  const [horizonte, setHorizonte] = useState(1);

  const horizonteValido =
    Number.isInteger(horizonte) &&
    horizonte >= HORIZONTE_MINIMO &&
    horizonte <= HORIZONTE_MAXIMO;

  const manejarCambioHorizonte = (e) => {
    const valor = e.target.value;

    if (valor === "") {
      setHorizonte("");
      return;
    }

    const numero = Number(valor);

    if (!Number.isNaN(numero)) {
      setHorizonte(numero);
    }
  };

  const manejarBlurHorizonte = () => {
    if (!horizonteValido) {
      setHorizonte(HORIZONTE_MINIMO);
    }
  };

  const intervaloRef = useRef(null);

  // Indica que nosotros iniciamos una actualización
  const solicitudEnCursoRef = useRef(false);

  // Evita ejecutar varias veces el callback de finalización
  const actualizacionSolicitadaRef = useRef(false);

  const detenerSeguimiento = useCallback(() => {
    if (intervaloRef.current) {
      clearInterval(intervaloRef.current);
      intervaloRef.current = null;
    }
  }, []);

  const consultarEstado = useCallback(async () => {
    try {
      const estado = await obtenerEstadoActualizacion();

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

        onActualizacionCompletada?.();
      }
    } catch (error) {
      console.error("Error consultando estado de actualización:", error);
    }
  }, [onUltimaActualizacion, onActualizacionCompletada, detenerSeguimiento]);

  const iniciarSeguimiento = useCallback(() => {
    if (intervaloRef.current) {
      return;
    }

    intervaloRef.current = setInterval(() => {
      consultarEstado();
    }, 1000);

    consultarEstado();
  }, [consultarEstado]);

  const ejecutarActualizacion = async () => {
    if (actualizando || !horizonteValido) {
      return;
    }

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
  }, [iniciarSeguimiento, detenerSeguimiento, onUltimaActualizacion]);

  return (
    <div className="control-actualizacion">
      <div className="control-actualizacion-tarjeta">
        <div className="control-actualizacion-fila">
          <div className="selector-horizonte">
            <label htmlFor="horizonte" className="sr-only">
              Horizonte en meses:
            </label>

            <span className="etiqueta-horizonte" aria-hidden="true">
              Meses
            </span>

            <input
              id="horizonte"
              type="number"
              inputMode="numeric"
              min={HORIZONTE_MINIMO}
              max={HORIZONTE_MAXIMO}
              step={1}
              title={`Horizonte de predicción en meses (${HORIZONTE_MINIMO}-${HORIZONTE_MAXIMO})`}
              value={horizonte}
              onChange={manejarCambioHorizonte}
              onBlur={manejarBlurHorizonte}
              disabled={actualizando}
            />
          </div>

          <button
            className={`boton-actualizar ${actualizando ? "actualizando" : ""}`}
            onClick={ejecutarActualizacion}
            disabled={actualizando || !horizonteValido}
          >
            <span className="icono-actualizar">{actualizando ? "↻" : "⟳"}</span>

            <span>{actualizando ? "Actualizando..." : "Actualizar datos"}</span>
          </button>
        </div>

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

              <span className="estado-texto-contenido">
                {estadoActualizacion.estado}
              </span>

              {estadoActualizacion.ejecutando && (
                <span className="progreso-porcentaje">
                  {estadoActualizacion.progreso || 0}%
                </span>
              )}
            </div>

            {estadoActualizacion.ejecutando && (
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
            )}

            {!estadoActualizacion.ejecutando &&
              estadoActualizacion.error &&
              estadoActualizacion.error !== estadoActualizacion.estado && (
                <div className="estado-detalle">
                  {estadoActualizacion.error}
                </div>
              )}
          </div>
        )}
      </div>
    </div>
  );
}

export default ControlActualizacion;
