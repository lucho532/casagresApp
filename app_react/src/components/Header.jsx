import { useRef, useState } from "react";
import { obtenerNombre, obtenerRol } from "../utils/auth";
import { obtenerIniciales } from "../utils/formato";
import { obtenerUrlCompleta, subirFotoPerfil } from "../services/api";
import "../styles/Header.css";

const TIPOS_PERMITIDOS = ["image/jpeg", "image/png", "image/webp"];
const TAMANO_MAXIMO_BYTES = 3 * 1024 * 1024;

function Header({
  paginaActual,
  ultimaActualizacion,
  fotoUrl,
  onFotoActualizada,
}) {
  const nombreUsuario = obtenerNombre();
  const rolUsuario = obtenerRol();

  const inputArchivoRef = useRef(null);

  // Si la URL de la foto falla al cargar, se cae de vuelta a las iniciales
  // en vez de mostrar un ícono de imagen rota.
  const [errorAlCargarFoto, setErrorAlCargarFoto] = useState(false);
  const [subiendo, setSubiendo] = useState(false);
  const [errorSubida, setErrorSubida] = useState("");

  const abrirSelectorDeArchivo = () => {
    if (!subiendo) {
      inputArchivoRef.current?.click();
    }
  };

  const manejarArchivoSeleccionado = async (e) => {
    const archivo = e.target.files?.[0];

    // Permite volver a elegir el mismo archivo más adelante (si no se
    // limpia, seleccionar el mismo archivo dos veces seguidas no dispara
    // otro evento "change").
    e.target.value = "";

    if (!archivo) {
      return;
    }

    setErrorSubida("");

    if (!TIPOS_PERMITIDOS.includes(archivo.type)) {
      setErrorSubida("Usa una imagen JPG, PNG o WEBP.");
      return;
    }

    if (archivo.size > TAMANO_MAXIMO_BYTES) {
      setErrorSubida("La imagen no puede superar los 3 MB.");
      return;
    }

    try {
      setSubiendo(true);
      setErrorAlCargarFoto(false);

      const respuesta = await subirFotoPerfil(archivo);

      onFotoActualizada?.(respuesta.fotoUrl);
    } catch (err) {
      console.error("Error al subir la foto de perfil:", err);

      setErrorSubida(
        err.response?.data?.mensaje || "No fue posible subir la foto.",
      );
    } finally {
      setSubiendo(false);
    }
  };

  const titulos = {
    inicio: "Inicio",
    tendencias: "Tendencias de compra",
    demanda: "Estimación de demanda futura",
    decisiones: "Enfoque en decisiones",
    powerbi: "Power BI",
    productos: "Productos",
    admin: "Administración",
  };

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

  const fechaFormateada = formatearFecha(ultimaActualizacion);

  return (
    <header className="header">
      <div className="header-titulo">
        <h1>{titulos[paginaActual] || "CASAGRES"}</h1>

        <p>Plataforma de analítica y predicción de demanda</p>
      </div>

      <div className="header-usuario">
        <div className="header-usuario-avatar-contenedor">
          <div className="header-usuario-icono">
            {fotoUrl && !errorAlCargarFoto ? (
              <img
                src={obtenerUrlCompleta(fotoUrl)}
                alt="Foto de perfil"
                className="header-usuario-foto"
                referrerPolicy="no-referrer"
                onError={() => setErrorAlCargarFoto(true)}
              />
            ) : (
              <span className="header-usuario-iniciales">
                {obtenerIniciales(nombreUsuario) || "👤"}
              </span>
            )}
          </div>

          <button
            type="button"
            className="header-usuario-boton-cambiar-foto"
            onClick={abrirSelectorDeArchivo}
            disabled={subiendo}
            title="Cambiar foto de perfil"
            aria-label="Cambiar foto de perfil"
          >
            {subiendo ? "…" : "📷"}
          </button>

          <input
            ref={inputArchivoRef}
            type="file"
            accept="image/jpeg,image/png,image/webp"
            style={{ display: "none" }}
            onChange={manejarArchivoSeleccionado}
          />
        </div>

        <div className="header-usuario-info">
          <span className="header-usuario-nombre">
            {nombreUsuario || "Usuario"}
          </span>

          <span className="header-usuario-rol">
            {rolUsuario === "admin" ? "Administrador" : "Usuario"}
          </span>

          {errorSubida && (
            <span className="header-usuario-error-foto">{errorSubida}</span>
          )}

          {!errorSubida && fechaFormateada && (
            <span className="header-ultima-actualizacion">
              Última actualización: {fechaFormateada}
            </span>
          )}
        </div>
      </div>
    </header>
  );
}

export default Header;
