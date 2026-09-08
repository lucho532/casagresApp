import { obtenerRol } from "../utils/auth";
import "../styles/Sidebar.css";
import ControlActualizacion from "./ControlActualizacion";

function Sidebar({
  paginaActual,
  cambiarPagina,
  cerrarSesion,
  onUltimaActualizacion,
  onActualizacionCompletada,
}) {
  const rol = obtenerRol();

  const opciones = [
    { id: "inicio", nombre: "Inicio", icono: "⌂" },
    { id: "tendencias", nombre: "Tendencias de compra", icono: "↗" },
    { id: "demanda", nombre: "Demanda futura", icono: "◷" },
    { id: "decisiones", nombre: "Enfoque en decisiones", icono: "✓" },
    { id: "powerbi", nombre: "Power BI", icono: "▦" },
    { id: "productos", nombre: "Productos", icono: "📦" },

    ...(rol === "admin"
      ? [
          {
            id: "admin",
            nombre: "Administración",
            icono: "⚙",
          },
        ]
      : []),
  ];

  return (
    <aside className="sidebar">
      <div className="sidebar-logo">
        <div className="logo-c">C</div>

        <div>
          <div className="logo-nombre">CASAGRES</div>

          <div className="logo-subtitulo">Analítica de ventas</div>
        </div>
      </div>

      <div className="sidebar-actualizacion">
        <ControlActualizacion
          onUltimaActualizacion={onUltimaActualizacion}
          onActualizacionCompletada={onActualizacionCompletada}
        />
      </div>

      <nav className="sidebar-menu">
        {opciones.map((opcion) => (
          <button
            key={opcion.id}
            className={`menu-item ${
              paginaActual === opcion.id ? "activo" : ""
            }`}
            onClick={() => cambiarPagina(opcion.id)}
          >
            <span className="menu-icono">{opcion.icono}</span>

            <span>{opcion.nombre}</span>
          </button>
        ))}
      </nav>

      <div className="sidebar-footer">
        <button
          type="button"
          className="boton-cerrar-sesion"
          onClick={cerrarSesion}
        >
          <span>↪</span>
          <span>Cerrar sesión</span>
        </button>

        <div className="sidebar-footer-text">
          Plataforma de Analítica
          <br />
          <span>CASAGRES</span>
        </div>
      </div>
    </aside>
  );
}

export default Sidebar;
