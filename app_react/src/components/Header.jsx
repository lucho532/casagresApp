import { useState } from "react";
import { obtenerNombre, obtenerRol } from "../utils/auth";
import "../styles/Header.css";
function Header({ paginaActual, ultimaActualizacion, fotoUrl }) {
  const nombreUsuario = obtenerNombre();
  const rolUsuario = obtenerRol();

  // Si la URL de la foto (viene de Google) falla al cargar -por ejemplo,
  // si expiró o el usuario la eliminó-, se cae de vuelta al ícono genérico
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
              src={fotoUrl}
              alt="Foto de perfil"
              className="header-usuario-foto"
              referrerPolicy="no-referrer"
              onError={() => setErrorAlCargarFoto(true)}
            />
          ) : (
            "👤"
          )}
        </div>

        <div className="header-usuario-info">
          <span className="header-usuario-nombre">
            {nombreUsuario || "Usuario"}
          </span>

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
