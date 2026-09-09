import { useEffect, useState } from "react";
import { obtenerDashboard } from "../services/api";

export function useDashboard(
  mensajeError = "No fue posible cargar la información de ventas.",
) {
  const [dashboard, setDashboard] = useState(null);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    let activo = true;

    const cargarDashboard = async () => {
      try {
        setCargando(true);
        setError("");

        const datos = await obtenerDashboard();

        if (activo) {
          setDashboard(datos);
        }
      } catch (err) {
        console.error(err);

        if (activo) {
          setError(mensajeError);
        }
      } finally {
        if (activo) {
          setCargando(false);
        }
      }
    };

    cargarDashboard();

    return () => {
      activo = false;
    };
  }, [mensajeError]);

  return { dashboard, cargando, error };
}
