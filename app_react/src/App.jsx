import { useState } from "react";

import Sidebar from "./components/Sidebar";
import Header from "./components/Header";

import Login from "./pages/Login";

import Inicio from "./pages/Inicio";
import Tendencias from "./pages/Tendencias";
import DemandaFutura from "./pages/DemandaFutura";
import Decisiones from "./pages/Decisiones";
import PowerBI from "./pages/PowerBI";
import Productos from "./pages/Productos";
import Administracion from "./pages/Administracion";
import { obtenerRol } from "./utils/auth";
import "./App.css";

function App() {
  const [autenticado, setAutenticado] = useState(
    !!localStorage.getItem("token"),
  );

  const [paginaActual, setPaginaActual] = useState("inicio");

  const cerrarSesion = () => {
    localStorage.removeItem("token");
    setAutenticado(false);
    setPaginaActual("inicio");
  };

  const renderizarPagina = () => {
    const rol = obtenerRol();

    // Protección del frontend:
    // un usuario normal no puede acceder a Administración
    if (paginaActual === "admin" && rol !== "admin") {
      return <Inicio cambiarPagina={setPaginaActual} />;
    }

    switch (paginaActual) {
      case "inicio":
        return <Inicio cambiarPagina={setPaginaActual} />;

      case "tendencias":
        return <Tendencias />;

      case "demanda":
        return <DemandaFutura />;

      case "decisiones":
        return <Decisiones />;

      case "powerbi":
        return <PowerBI />;

      case "productos":
        return <Productos />;

      case "admin":
        return <Administracion />;

      default:
        return <Inicio cambiarPagina={setPaginaActual} />;
    }
  };

  if (!autenticado) {
    return <Login iniciarSesionCorrectamente={() => setAutenticado(true)} />;
  }

  return (
    <div className="app">
      <Sidebar
        paginaActual={paginaActual}
        cambiarPagina={setPaginaActual}
        cerrarSesion={cerrarSesion}
      />

      <div className="contenido-principal">
        <Header paginaActual={paginaActual} />

        <main className="contenido">{renderizarPagina()}</main>
      </div>
    </div>
  );
}

export default App;
