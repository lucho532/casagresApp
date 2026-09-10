import { useEffect, useMemo, useState } from "react";
import {
  obtenerTablerosPowerBi,
  agregarTableroPowerBi,
  eliminarTableroPowerBi,
} from "../services/api";
import { obtenerRol } from "../utils/auth";
import EstadoCargando from "../components/EstadoCargando";
import MensajeError from "../components/MensajeError";
import "../styles/PowerBI.css";

function PowerBI() {
  const esAdmin = obtenerRol() === "admin";

  const [tableros, setTableros] = useState([]);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState("");

  const [tableroSeleccionadoId, setTableroSeleccionadoId] = useState(null);

  const [mostrarFormulario, setMostrarFormulario] = useState(false);
  const [nombreNuevo, setNombreNuevo] = useState("");
  const [urlNueva, setUrlNueva] = useState("");
  const [agregando, setAgregando] = useState(false);
  const [errorFormulario, setErrorFormulario] = useState("");

  const [eliminandoId, setEliminandoId] = useState(null);

  // =========================================
  // CARGAR TABLEROS (el más reciente primero)
  // =========================================

  const cargarTableros = async ({ seleccionarMasReciente } = {}) => {
    try {
      setCargando(true);
      setError("");

      const datos = await obtenerTablerosPowerBi();

      setTableros(datos || []);

      if (seleccionarMasReciente && datos?.length > 0) {
        setTableroSeleccionadoId(datos[0].id);
      }

      return datos || [];
    } catch (err) {
      console.error(err);
      setError("No fue posible cargar los tableros de Power BI.");
      return [];
    } finally {
      setCargando(false);
    }
  };

  useEffect(() => {
    cargarTableros({ seleccionarMasReciente: true });
  }, []);

  // =========================================
  // TABLERO SELECCIONADO
  // =========================================

  const tableroSeleccionado = useMemo(
    () => tableros.find((t) => t.id === tableroSeleccionadoId),
    [tableros, tableroSeleccionadoId],
  );

  // =========================================
  // AGREGAR TABLERO
  // =========================================

  const manejarAgregarTablero = async (evento) => {
    evento.preventDefault();

    if (!nombreNuevo.trim() || !urlNueva.trim()) {
      setErrorFormulario("El nombre y la URL son obligatorios.");
      return;
    }

    try {
      setAgregando(true);
      setErrorFormulario("");

      const nuevoTablero = await agregarTableroPowerBi(
        nombreNuevo.trim(),
        urlNueva.trim(),
      );

      await cargarTableros();

      setTableroSeleccionadoId(nuevoTablero.id);
      setNombreNuevo("");
      setUrlNueva("");
      setMostrarFormulario(false);
    } catch (err) {
      console.error(err);

      setErrorFormulario(
        err.response?.data?.mensaje ||
          "No fue posible agregar el tablero. Verifica la URL e inténtalo de nuevo.",
      );
    } finally {
      setAgregando(false);
    }
  };

  // =========================================
  // ELIMINAR TABLERO
  // =========================================

  const manejarEliminarTablero = async (tablero) => {
    const confirmar = window.confirm(
      `¿Eliminar el tablero "${tablero.nombre}"? Esta acción no se puede deshacer.`,
    );

    if (!confirmar) {
      return;
    }

    try {
      setEliminandoId(tablero.id);

      await eliminarTableroPowerBi(tablero.id);

      const restantes = await cargarTableros();

      if (tableroSeleccionadoId === tablero.id) {
        setTableroSeleccionadoId(restantes[0]?.id ?? null);
      }
    } catch (err) {
      console.error(err);
      window.alert("No fue posible eliminar el tablero.");
    } finally {
      setEliminandoId(null);
    }
  };

  // =========================================
  // CARGANDO / ERROR
  // =========================================

  if (cargando) {
    return <EstadoCargando mensaje="Cargando tableros de Power BI..." />;
  }

  if (error) {
    return <MensajeError mensaje={error} />;
  }

  return (
    <div className="pagina powerbi">
      {/* =====================================
          ENCABEZADO
      ====================================== */}

      <div className="pagina-encabezado powerbi-encabezado">
        <div>
          <span className="powerbi-etiqueta">INTELIGENCIA DE NEGOCIO</span>

          <h2>Power BI</h2>

          <p>Consulta los informes y análisis interactivos de CASAGRES.</p>
        </div>
      </div>

      {/* =====================================
          BARRA DE TABLEROS
      ====================================== */}

      <div className="powerbi-barra">
        <label className="powerbi-selector">
          <span>Tablero</span>

          <select
            value={tableroSeleccionadoId ?? ""}
            onChange={(e) => setTableroSeleccionadoId(Number(e.target.value))}
            disabled={tableros.length === 0}
          >
            {tableros.length === 0 && <option value="">Sin tableros</option>}

            {tableros.map((tablero) => (
              <option key={tablero.id} value={tablero.id}>
                {tablero.nombre}
              </option>
            ))}
          </select>
        </label>

        {esAdmin && (
          <div className="powerbi-acciones">
            {tableroSeleccionado && (
              <button
                type="button"
                className="powerbi-boton-eliminar"
                onClick={() => manejarEliminarTablero(tableroSeleccionado)}
                disabled={eliminandoId === tableroSeleccionado.id}
              >
                {eliminandoId === tableroSeleccionado.id
                  ? "Eliminando..."
                  : "Eliminar tablero"}
              </button>
            )}

            <button
              type="button"
              className="powerbi-boton-agregar"
              onClick={() => setMostrarFormulario((actual) => !actual)}
            >
              {mostrarFormulario ? "Cancelar" : "+ Agregar tablero"}
            </button>
          </div>
        )}
      </div>

      {/* =====================================
          FORMULARIO PARA AGREGAR TABLERO
      ====================================== */}

      {esAdmin && mostrarFormulario && (
        <form className="powerbi-formulario" onSubmit={manejarAgregarTablero}>
          <div className="powerbi-formulario-campos">
            <label>
              <span>Nombre</span>

              <input
                type="text"
                value={nombreNuevo}
                onChange={(e) => setNombreNuevo(e.target.value)}
                placeholder="Ej. Ventas por zona"
              />
            </label>

            <label>
              <span>URL del tablero</span>

              <input
                type="text"
                value={urlNueva}
                onChange={(e) => setUrlNueva(e.target.value)}
                placeholder="https://app.powerbi.com/view?r=..."
              />
            </label>
          </div>

          {errorFormulario && (
            <div className="powerbi-formulario-error">{errorFormulario}</div>
          )}

          <button type="submit" disabled={agregando}>
            {agregando ? "Agregando..." : "Guardar tablero"}
          </button>
        </form>
      )}

      {/* =====================================
          INFORME POWER BI
      ====================================== */}

      {tableroSeleccionado ? (
        <div className="powerbi-contenedor-real">
          <iframe
            title={tableroSeleccionado.nombre}
            src={tableroSeleccionado.url}
            frameBorder="0"
            allowFullScreen
          />
        </div>
      ) : (
        <div className="powerbi-contenedor">
          <div className="powerbi-placeholder">
            <div className="powerbi-icono">▦</div>

            <h3>No hay tableros disponibles</h3>

            <p>
              {esAdmin
                ? "Agrega la URL de un tablero de Power BI para empezar a visualizarlo aquí."
                : "Pídele a un administrador que agregue un tablero de Power BI."}
            </p>
          </div>
        </div>
      )}
    </div>
  );
}

export default PowerBI;
