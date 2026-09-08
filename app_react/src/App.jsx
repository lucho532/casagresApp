import { useEffect, useState } from "react";

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
import { obtenerDashboard } from "./services/api";
import { obtenerRol } from "./utils/auth";

import "./App.css";

function App() {
  const [autenticado, setAutenticado] = useState(
    !!localStorage.getItem("token"),
  );

  const [paginaActual, setPaginaActual] = useState("inicio");
  const [mesSeleccionado, setMesSeleccionado] = useState("");
  const [mesesDisponibles, setMesesDisponibles] = useState([]);

  const [ultimaActualizacion, setUltimaActualizacion] = useState(null);

  useEffect(() => {
    const cargarMeses = async () => {
      try {
        const datos = await obtenerDashboard();

        const meses = datos?.meses ?? [];

        setMesesDisponibles(meses);

        if (meses.length > 0) {
          setMesSeleccionado(meses[0].mes);
        }
      } catch (error) {
        console.error("No fue posible cargar los meses:", error);
      }
    };

    if (autenticado) {
      cargarMeses();
    }
  }, [autenticado]);

  const cerrarSesion = () => {
    localStorage.removeItem("token");
    setAutenticado(false);
    setPaginaActual("inicio");
  };

  const renderizarPagina = () => {
    const rol = obtenerRol();

    if (paginaActual === "admin" && rol !== "admin") {
      return <Inicio cambiarPagina={setPaginaActual} />;
    }

    switch (paginaActual) {
      case "inicio":
        return (
          <Inicio
            cambiarPagina={setPaginaActual}
            mesSeleccionado={mesSeleccionado}
            setMesSeleccionado={setMesSeleccionado}
            mesesDisponibles={mesesDisponibles}
          />
        );

      case "tendencias":
        return <Tendencias mesSeleccionado={mesSeleccionado} />;

      case "demanda":
        return <DemandaFutura mesSeleccionado={mesSeleccionado} />;

      case "decisiones":
        return <Decisiones mesSeleccionado={mesSeleccionado} />;

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

  const recargarDashboard = () => {
    window.location.reload();
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
        onUltimaActualizacion={setUltimaActualizacion}
        onActualizacionCompletada={recargarDashboard}
      />

      <div className="contenido-principal">
        <Header
          paginaActual={paginaActual}
          ultimaActualizacion={ultimaActualizacion}
        />

        <main className="contenido">{renderizarPagina()}</main>
      </div>
    </div>
  );
}

export default App;
