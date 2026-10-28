import { useState } from "react";
import { obtenerNombre, obtenerRol } from "../utils/auth";
import { obtenerIniciales } from "../utils/formato";
import { obtenerUrlCompleta } from "../services/api";
import "../styles/Header.css";

function Header({ paginaActual, ultimaActualizacion, fotoUrl, onAbrirPerfil }) {
  const nombreUsuario = obtenerNombre();
  const rolUsuario = obtenerRol();

  // Si la URL de la foto falla al cargar, se cae de vuelta a las iniciales
  // en vez de mostrar un ícono de imagen rota.
  const [errorAlCargarFoto, setErrorAlCargarFoto] = useState(false);

  const titulos = {
    inicio: "Inicio",
    tendencias: "Tendencias de compra",
    demanda: "Estimación de demanda futura",
    decisiones: "Enfoque en decisiones",
    powerbi: "Power BI",
    productos: "Productos",
    admin: "Administración",
    perfil: "Mi perfil",
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

        <div className="header-usuario-info">
          <button
            type="button"
            className="header-usuario-nombre header-usuario-nombre-boton"
            onClick={onAbrirPerfil}
            title="Editar mi perfil"
          >
            {nombreUsuario || "Usuario"}
          </button>

          <span className="header-usuario-rol">
            {rolUsuario === "admin" ? "Administrador" : "Usuario"}
          </span>

          {fechaFormateada && (
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
