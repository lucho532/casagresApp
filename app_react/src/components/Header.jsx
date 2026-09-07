import { useEffect, useRef, useState } from "react";

import { actualizarDatos, obtenerEstadoActualizacion } from "../services/api";

function Header({ paginaActual }) {
  const [actualizando, setActualizando] = useState(false);
  const [estadoActualizacion, setEstadoActualizacion] = useState(null);

  const intervaloRef = useRef(null);

  const titulos = {
    inicio: "Inicio",
    tendencias: "Tendencias de compra",
    demanda: "Estimación de demanda futura",
    decisiones: "Enfoque en decisiones",
    powerbi: "Power BI",
  };

  // --------------------------------------------------
  // CONSULTAR ESTADO
  // --------------------------------------------------

  const consultarEstado = async () => {
    try {
      const estado = await obtenerEstadoActualizacion();

      console.log("Estado del pipeline:", estado);

      setEstadoActualizacion(estado);

      if (estado.ejecutando) {
        setActualizando(true);
      } else {
        setActualizando(false);

        detenerSeguimiento();
      }
    } catch (error) {
      console.error("Error consultando estado de actualización:", error);
    }
  };

  // --------------------------------------------------
  // INICIAR SEGUIMIENTO
  // --------------------------------------------------

  const iniciarSeguimiento = () => {
    if (intervaloRef.current) {
      return;
    }

    consultarEstado();

    intervaloRef.current = setInterval(() => {
      consultarEstado();
    }, 1000);
  };

  // --------------------------------------------------
  // DETENER SEGUIMIENTO
  // --------------------------------------------------

  const detenerSeguimiento = () => {
    if (intervaloRef.current) {
      clearInterval(intervaloRef.current);
      intervaloRef.current = null;
    }
  };

  // --------------------------------------------------
  // EJECUTAR ACTUALIZACIÓN
  // --------------------------------------------------

  const ejecutarActualizacion = async () => {
    if (actualizando) {
      return;
    }

    setActualizando(true);

    try {
      iniciarSeguimiento();

      await actualizarDatos();

      await consultarEstado();
    } catch (error) {
      console.error("Error iniciando actualización:", error);

      if (error.response?.status === 409) {
        setActualizando(true);

        setEstadoActualizacion({
          ejecutando: true,
          estado: "Ya hay una actualización en curso.",
          progreso: 0,
        });

        iniciarSeguimiento();
      } else {
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
    }
  };

  // --------------------------------------------------
  // COMPROBAR ESTADO AL ABRIR
  // --------------------------------------------------

  useEffect(() => {
    const comprobarActualizacion = async () => {
      try {
        const estado = await obtenerEstadoActualizacion();

        console.log("Estado inicial del pipeline:", estado);

        setEstadoActualizacion(estado);

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
      detenerSeguimiento();
    };
  }, []);

  // --------------------------------------------------
  // FORMATEAR FECHA
  // --------------------------------------------------

  const formatearFecha = (fecha) => {
    if (!fecha) {
      return null;
    }

    const fechaObj = new Date(fecha);

    if (isNaN(fechaObj.getTime())) {
      return null;
    }

    return fechaObj.toLocaleString("es-CO", {
      day: "2-digit",
      month: "2-digit",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  const ultimaActualizacion = formatearFecha(
    estadoActualizacion?.ultimaActualizacion,
  );

  return (
    <header className="header">
      {/* -------------------------------------------- */}
      {/* TITULO */}
      {/* -------------------------------------------- */}

      <div className="header-titulo">
        <h1>{titulos[paginaActual] || "CASAGRES"}</h1>

        <p>Plataforma de analítica y predicción de demanda</p>
      </div>

      {/* -------------------------------------------- */}
      {/* ACTUALIZACIÓN */}
      {/* -------------------------------------------- */}

      <div className="header-actualizacion">
        <button
          className={`boton-actualizar ${actualizando ? "actualizando" : ""}`}
          onClick={ejecutarActualizacion}
          disabled={actualizando}
        >
          <span className="icono-actualizar">{actualizando ? "↻" : "⟳"}</span>

          <span>{actualizando ? "Actualizando..." : "Actualizar datos"}</span>
        </button>

        {/* ---------------------------------------- */}
        {/* ESTADO ACTUAL */}
        {/* ---------------------------------------- */}

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

            {/* ------------------------------------ */}
            {/* BARRA DE PROGRESO */}
            {/* ------------------------------------ */}

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

            {/* ------------------------------------ */}
            {/* ÚLTIMA ACTUALIZACIÓN */}
            {/* ------------------------------------ */}

            {ultimaActualizacion && (
              <div className="ultima-actualizacion">
                <span className="ultima-actualizacion-label">
                  Última actualización:
                </span>

                <span className="ultima-actualizacion-fecha">
                  {ultimaActualizacion}
                </span>
              </div>
            )}
          </div>
        )}
      </div>
    </header>
  );
}

export default Header;
