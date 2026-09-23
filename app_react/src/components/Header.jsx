import { obtenerNombre, obtenerRol } from "../utils/auth";
import "../styles/Header.css";
function Header({ paginaActual, ultimaActualizacion }) {
  const nombreUsuario = obtenerNombre();
  const rolUsuario = obtenerRol();

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
        <div className="header-usuario-icono">👤</div>

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
