import { useEffect, useState } from "react";

import Sidebar from "./components/Sidebar";
import Header from "./components/Header";

import Login from "./pages/Login";
import CuentaPendiente from "./pages/CuentaPendiente";

import Inicio from "./pages/Inicio";
import Tendencias from "./pages/Tendencias";
import DemandaFutura from "./pages/DemandaFutura";
import Decisiones from "./pages/Decisiones";
import PowerBI from "./pages/PowerBI";
import Productos from "./pages/Productos";
import Administracion from "./pages/Administracion";
import EstadoCargando from "./components/EstadoCargando";
import { obtenerDashboard, obtenerPerfil } from "./services/api";
import { obtenerRol } from "./utils/auth";

import "./App.css";

const ROL_PENDIENTE = "pendiente";

// Mientras la cuenta esté pendiente de aprobación, se vuelve a consultar
// el perfil con esta frecuencia para detectar el cambio de rol sin que
// el usuario tenga que recargar la página.
const INTERVALO_SONDEO_PERFIL_MS = 5000;

function App() {
  const [autenticado, setAutenticado] = useState(
    !!sessionStorage.getItem("token"),
  );

  const [paginaActual, setPaginaActual] = useState("inicio");
  const [mesSeleccionado, setMesSeleccionado] = useState("");
  const [mesesDisponibles, setMesesDisponibles] = useState([]);

  const [ultimaActualizacion, setUltimaActualizacion] = useState(null);

  // El rol embebido en el JWT queda "congelado" al iniciar sesión y es
  // válido hasta por 2 horas. El perfil, en cambio, se consulta al
  // backend y siempre refleja el rol y el estado actuales.
  const [perfil, setPerfil] = useState(null);
  const [cargandoPerfil, setCargandoPerfil] = useState(true);

  useEffect(() => {
    if (!autenticado) {
      setPerfil(null);
      setCargandoPerfil(false);
      return;
    }

    let activo = true;

    const cargarPerfil = async () => {
      try {
        const datos = await obtenerPerfil();

        if (activo) {
          setPerfil(datos);
        }
      } catch (error) {
        console.error("No fue posible obtener el perfil:", error);
      } finally {
        if (activo) {
          setCargandoPerfil(false);
        }
      }
    };

    cargarPerfil();

    return () => {
      activo = false;
    };
  }, [autenticado]);

  useEffect(() => {
    if (perfil?.rol !== ROL_PENDIENTE) {
      return;
    }

    let activo = true;

    const intervalo = setInterval(async () => {
      try {
        const datos = await obtenerPerfil();

        if (activo) {
          setPerfil(datos);
        }
      } catch (error) {
        console.error("No fue posible actualizar el perfil:", error);
      }
    }, INTERVALO_SONDEO_PERFIL_MS);

    return () => {
      activo = false;
      clearInterval(intervalo);
    };
  }, [perfil?.rol]);

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

    if (autenticado && perfil?.rol && perfil.rol !== ROL_PENDIENTE) {
      cargarMeses();
    }
  }, [autenticado, perfil?.rol]);

  const cerrarSesion = () => {
    sessionStorage.removeItem("token");
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
        return (
          <Tendencias
            mesSeleccionado={mesSeleccionado}
            mesesDisponibles={mesesDisponibles}
            setMesSeleccionado={setMesSeleccionado}
          />
        );

      case "demanda":
        return (
          <DemandaFutura
            mesSeleccionado={mesSeleccionado}
            mesesDisponibles={mesesDisponibles}
            setMesSeleccionado={setMesSeleccionado}
          />
        );

      case "decisiones":
        return (
          <Decisiones
            mesSeleccionado={mesSeleccionado}
            mesesDisponibles={mesesDisponibles}
            setMesSeleccionado={setMesSeleccionado}
          />
        );

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

  if (cargandoPerfil) {
    return <EstadoCargando mensaje="Cargando tu cuenta..." />;
  }

  if (perfil?.rol === ROL_PENDIENTE) {
    return <CuentaPendiente onCerrarSesion={cerrarSesion} />;
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
