import { useEffect, useRef, useState } from "react";
import { obtenerUrlCompleta, subirFotoPerfil } from "../services/api";
import { obtenerIniciales } from "../utils/formato";
import "../styles/Perfil.css";

const TIPOS_PERMITIDOS = ["image/jpeg", "image/png", "image/webp"];
const TAMANO_MAXIMO_BYTES = 3 * 1024 * 1024;

function Perfil({ perfil, onFotoActualizada }) {
  const inputArchivoRef = useRef(null);

  const [errorAlCargarFoto, setErrorAlCargarFoto] = useState(false);
  const [subiendo, setSubiendo] = useState(false);
  const [errorSubida, setErrorSubida] = useState("");
  const [fotoAmpliada, setFotoAmpliada] = useState(false);

  useEffect(() => {
    if (!fotoAmpliada) {
      return;
    }

    const cerrarConEscape = (e) => {
      if (e.key === "Escape") {
        setFotoAmpliada(false);
      }
    };

    document.addEventListener("keydown", cerrarConEscape);

    return () => document.removeEventListener("keydown", cerrarConEscape);
  }, [fotoAmpliada]);

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

  const fotoUrl = perfil?.fotoUrl;
  const nombre = perfil?.nombre;

  return (
    <div className="pagina perfil">
      <div className="pagina-encabezado">
        <div>
          <h2>Mi perfil</h2>

          <p>Consulta tus datos y actualiza tu foto de perfil.</p>
        </div>
      </div>

      <div className="perfil-tarjeta">
        <div className="perfil-avatar-seccion">
          <div
            className={`perfil-avatar ${
              fotoUrl && !errorAlCargarFoto ? "perfil-avatar-ampliable" : ""
            }`}
            onClick={() => {
              if (fotoUrl && !errorAlCargarFoto) {
                setFotoAmpliada(true);
              }
            }}
            role={fotoUrl && !errorAlCargarFoto ? "button" : undefined}
            tabIndex={fotoUrl && !errorAlCargarFoto ? 0 : undefined}
            onKeyDown={(e) => {
              if (
                fotoUrl &&
                !errorAlCargarFoto &&
                (e.key === "Enter" || e.key === " ")
              ) {
                e.preventDefault();
                setFotoAmpliada(true);
              }
            }}
            aria-label={
              fotoUrl && !errorAlCargarFoto ? "Ver foto de perfil en grande" : undefined
            }
          >
            {fotoUrl && !errorAlCargarFoto ? (
              <img
                src={obtenerUrlCompleta(fotoUrl)}
                alt="Foto de perfil"
                className="perfil-avatar-foto"
                referrerPolicy="no-referrer"
                onError={() => setErrorAlCargarFoto(true)}
              />
            ) : (
              <span className="perfil-avatar-iniciales">
                {obtenerIniciales(nombre) || "👤"}
              </span>
            )}
          </div>

          <button
            type="button"
            className="perfil-boton-cambiar-foto"
            onClick={abrirSelectorDeArchivo}
            disabled={subiendo}
          >
            {subiendo ? "Subiendo..." : "Cambiar foto"}
          </button>

          <input
            ref={inputArchivoRef}
            type="file"
            accept="image/jpeg,image/png,image/webp"
            style={{ display: "none" }}
            onChange={manejarArchivoSeleccionado}
          />

          {errorSubida && (
            <span className="perfil-error-foto">{errorSubida}</span>
          )}
        </div>

        <div className="perfil-info">
          <div className="perfil-campo">
            <span>Nombre</span>

            <strong>{nombre || "-"}</strong>
          </div>

          <div className="perfil-campo">
            <span>Correo electrónico</span>

            <strong>{perfil?.email || "-"}</strong>
          </div>

          <div className="perfil-campo">
            <span>Rol</span>

            <strong>{perfil?.rol === "admin" ? "Administrador" : "Usuario"}</strong>
          </div>
        </div>
      </div>

      {fotoAmpliada && fotoUrl && (
        <div
          className="perfil-foto-overlay"
          onClick={() => setFotoAmpliada(false)}
        >
          <button
            type="button"
            className="perfil-foto-cerrar"
            onClick={() => setFotoAmpliada(false)}
            aria-label="Cerrar"
          >
            ×
          </button>

          <img
            src={obtenerUrlCompleta(fotoUrl)}
            alt="Foto de perfil ampliada"
            className="perfil-foto-ampliada"
            referrerPolicy="no-referrer"
          />
        </div>
      )}
    </div>
  );
}

export default Perfil;
