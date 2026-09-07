import { useEffect, useState } from "react";
import { obtenerUsuarios, cambiarRol, cambiarEstado } from "../services/api";
import "../styles/Administracion.css";

function Administracion() {
  const [usuarios, setUsuarios] = useState([]);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState("");
  const [cambiandoRol, setCambiandoRol] = useState(null);
  const [cambiandoEstado, setCambiandoEstado] = useState(null);

  useEffect(() => {
    cargarUsuarios();
  }, []);

  const cargarUsuarios = async () => {
    try {
      setCargando(true);
      setError("");

      const datos = await obtenerUsuarios();

      setUsuarios(datos);
    } catch (err) {
      console.error("Error al obtener usuarios:", err);

      if (err.response?.status === 403) {
        setError("No tienes permisos para acceder a esta sección.");
      } else {
        setError("No fue posible cargar los usuarios.");
      }
    } finally {
      setCargando(false);
    }
  };

  const manejarCambioRol = async (usuario) => {
    const nuevoRol = usuario.rol === "admin" ? "usuario" : "admin";

    const confirmar = window.confirm(
      `¿Quieres cambiar el rol de "${usuario.usuario}" a "${nuevoRol}"?`,
    );

    if (!confirmar) {
      return;
    }

    try {
      setCambiandoRol(usuario.id);

      await cambiarRol(usuario.id, nuevoRol);

      setUsuarios((usuariosActuales) =>
        usuariosActuales.map((u) =>
          u.id === usuario.id ? { ...u, rol: nuevoRol } : u,
        ),
      );
    } catch (err) {
      console.error("Error al cambiar el rol:", err);

      if (err.response?.status === 400) {
        alert(err.response.data?.mensaje || "No se pudo cambiar el rol.");
      } else if (err.response?.status === 403) {
        alert("No tienes permisos para realizar esta acción.");
      } else {
        alert("No fue posible cambiar el rol.");
      }
    } finally {
      setCambiandoRol(null);
    }
  };

  const manejarCambioEstado = async (usuario) => {
    const nuevoEstado = !usuario.activo;

    const accion = nuevoEstado ? "activar" : "desactivar";

    const confirmar = window.confirm(
      `¿Quieres ${accion} al usuario "${usuario.usuario}"?`,
    );

    if (!confirmar) {
      return;
    }

    try {
      setCambiandoEstado(usuario.id);

      await cambiarEstado(usuario.id, nuevoEstado);

      setUsuarios((usuariosActuales) =>
        usuariosActuales.map((u) =>
          u.id === usuario.id ? { ...u, activo: nuevoEstado } : u,
        ),
      );
    } catch (err) {
      console.error("Error al cambiar el estado:", err);

      if (err.response?.status === 400) {
        alert(
          err.response.data?.mensaje ||
            "No se pudo cambiar el estado del usuario.",
        );
      } else if (err.response?.status === 403) {
        alert("No tienes permisos para realizar esta acción.");
      } else {
        alert("No fue posible cambiar el estado del usuario.");
      }
    } finally {
      setCambiandoEstado(null);
    }
  };

  if (cargando) {
    return (
      <div className="administracion">
        <div className="administracion-header">
          <h1>Administración</h1>
          <p>Gestión de usuarios de la plataforma.</p>
        </div>

        <div className="administracion-card">
          <div className="administracion-mensaje">Cargando usuarios...</div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="administracion">
        <div className="administracion-header">
          <h1>Administración</h1>
          <p>Gestión de usuarios de la plataforma.</p>
        </div>

        <div className="administracion-error">{error}</div>
      </div>
    );
  }

  return (
    <div className="administracion">
      <div className="administracion-header">
        <h1>Administración</h1>
        <p>Gestión de usuarios de la plataforma.</p>
      </div>

      <div className="administracion-card">
        <div className="administracion-card-header">
          <h2>Usuarios</h2>

          <span className="administracion-contador">
            {usuarios.length} usuario
            {usuarios.length !== 1 ? "s" : ""}
          </span>
        </div>

        <div className="administracion-tabla-contenedor">
          <table className="administracion-tabla">
            <thead>
              <tr>
                <th>ID</th>
                <th>Usuario</th>
                <th>Nombre</th>
                <th>Email</th>
                <th>Rol</th>
                <th>Estado</th>
                <th>Fecha creación</th>
                <th>Acciones</th>
              </tr>
            </thead>

            <tbody>
              {usuarios.map((usuario) => (
                <tr key={usuario.id}>
                  <td>
                    <span className="usuario-id">#{usuario.id}</span>
                  </td>

                  <td>
                    <span className="usuario-nombre">{usuario.usuario}</span>
                  </td>

                  <td>{usuario.nombre || "—"}</td>

                  <td>{usuario.email || "—"}</td>

                  <td>
                    <span
                      className={`badge-rol ${
                        usuario.rol === "admin"
                          ? "badge-admin"
                          : "badge-usuario"
                      }`}
                    >
                      {usuario.rol === "admin" ? "Administrador" : "Usuario"}
                    </span>
                  </td>

                  <td>
                    <span
                      className={`badge-estado ${
                        !usuario.activo ? "badge-inactivo" : ""
                      }`}
                    >
                      {usuario.activo ? "Activo" : "Inactivo"}
                    </span>
                  </td>

                  <td>
                    {new Date(usuario.fechaCreacion).toLocaleDateString()}
                  </td>

                  <td>
                    <div className="acciones-usuario">
                      <button
                        type="button"
                        className="boton-cambiar-rol"
                        onClick={() => manejarCambioRol(usuario)}
                        disabled={cambiandoRol === usuario.id}
                      >
                        {cambiandoRol === usuario.id
                          ? "Guardando..."
                          : usuario.rol === "admin"
                            ? "Hacer usuario"
                            : "Hacer admin"}
                      </button>

                      <button
                        type="button"
                        className={`boton-cambiar-estado ${
                          usuario.activo ? "boton-desactivar" : "boton-activar"
                        }`}
                        onClick={() => manejarCambioEstado(usuario)}
                        disabled={cambiandoEstado === usuario.id}
                      >
                        {cambiandoEstado === usuario.id
                          ? "Guardando..."
                          : usuario.activo
                            ? "Desactivar"
                            : "Activar"}
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

export default Administracion;
