// Pantalla de autenticación y registro. La validación real de
// credenciales ocurre en el backend; aquí solo se gestiona el
// formulario y se persiste el JWT que devuelve la API.

import { useEffect, useState } from "react";
import {
  iniciarSesion,
  registrarUsuario,
  iniciarSesionMicrosoft,
} from "../services/api";
import { useMsal } from "@azure/msal-react";
import { loginRequest } from "../authConfig";
import "../styles/Login.css";

function Login({ iniciarSesionCorrectamente }) {
  const [modoRegistro, setModoRegistro] = useState(false);

  const [usuario, setUsuario] = useState("");
  const [password, setPassword] = useState("");
  const [nombre, setNombre] = useState("");
  const [email, setEmail] = useState("");

  const [cargando, setCargando] = useState(false);
  const [error, setError] = useState("");
  const [mensaje, setMensaje] = useState("");

  const { instance } = useMsal();

  useEffect(() => {
    const procesarLoginMicrosoft = async () => {
      const idToken = sessionStorage.getItem("microsoft_id_token");

      if (!idToken) {
        return;
      }

      try {
        setCargando(true);
        setError("");
        setMensaje("");

        const respuesta = await iniciarSesionMicrosoft(idToken);

        if (!respuesta.token) {
          throw new Error("El servidor no devolvió un token de CASAGRES.");
        }

        localStorage.setItem("token", respuesta.token);

        sessionStorage.removeItem("microsoft_id_token");

        iniciarSesionCorrectamente();
      } catch (err) {
        console.error("Error al completar login Microsoft:", err);

        sessionStorage.removeItem("microsoft_id_token");

        setError(
          err.response?.data?.mensaje ||
            "No fue posible iniciar sesión con Microsoft.",
        );
      } finally {
        setCargando(false);
      }
    };

    procesarLoginMicrosoft();
  }, [iniciarSesionCorrectamente]);

  const manejarLogin = async (e) => {
    e.preventDefault();

    setError("");
    setMensaje("");

    if (!usuario.trim() || !password.trim()) {
      setError("Usuario y contraseña son obligatorios.");
      return;
    }

    try {
      setCargando(true);

      const respuesta = await iniciarSesion(usuario, password);

      localStorage.setItem("token", respuesta.token);

      iniciarSesionCorrectamente();
    } catch (err) {
      console.error(err);

      if (err.response?.status === 401) {
        setError("Usuario o contraseña incorrectos.");
      } else {
        setError("No fue posible conectar con el servidor.");
      }
    } finally {
      setCargando(false);
    }
  };

  const manejarLoginMicrosoft = async () => {
    try {
      setCargando(true);
      setError("");
      setMensaje("");

      await instance.loginRedirect(loginRequest);
    } catch (err) {
      console.error("Error al iniciar sesión con Microsoft:", err);

      setError("No fue posible iniciar sesión con Microsoft.");

      setCargando(false);
    }
  };

  const manejarRegistro = async (e) => {
    e.preventDefault();

    setError("");
    setMensaje("");

    if (
      !usuario.trim() ||
      !password.trim() ||
      !nombre.trim() ||
      !email.trim()
    ) {
      setError(
        "Usuario, contraseña, nombre y correo electrónico son obligatorios.",
      );
      return;
    }

    try {
      setCargando(true);

      await registrarUsuario(usuario, password, nombre, email);

      setMensaje("Usuario creado correctamente. Ya puedes iniciar sesión.");

      setModoRegistro(false);

      setPassword("");
      setNombre("");
      setEmail("");
    } catch (err) {
      console.error(err);

      if (err.response?.status === 409) {
        setError("El usuario o correo electrónico ya existe.");
      } else {
        setError("No fue posible crear el usuario.");
      }
    } finally {
      setCargando(false);
    }
  };

  const cambiarModo = () => {
    setModoRegistro(!modoRegistro);

    setError("");
    setMensaje("");

    setUsuario("");
    setPassword("");
    setNombre("");
    setEmail("");
  };

  return (
    <div className="login-pagina">
      <div className="login-tarjeta">
        <div className="login-logo">
          <div className="logo-c">C</div>
        </div>

        <h1>CASAGRES</h1>

        <p className="login-subtitulo">
          Plataforma de Analítica & Predicción de Demanda
        </p>

        {modoRegistro ? (
          <form onSubmit={manejarRegistro}>
            <div className="login-campo">
              <label>Nombre</label>

              <input
                type="text"
                value={nombre}
                onChange={(e) => setNombre(e.target.value)}
                placeholder="Ingrese su nombre"
                autoComplete="name"
              />
            </div>

            <div className="login-campo">
              <label>Correo electrónico</label>

              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="Ingrese su correo electrónico"
                autoComplete="email"
              />
            </div>

            <div className="login-campo">
              <label>Usuario</label>

              <input
                type="text"
                value={usuario}
                onChange={(e) => setUsuario(e.target.value)}
                placeholder="Ingrese su usuario"
                autoComplete="username"
              />
            </div>

            <div className="login-campo">
              <label>Contraseña</label>

              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Ingrese su contraseña"
                autoComplete="new-password"
              />
            </div>

            {error && <div className="login-error">{error}</div>}

            {mensaje && <div className="login-mensaje">{mensaje}</div>}

            <button type="submit" className="login-boton" disabled={cargando}>
              {cargando ? "Creando usuario..." : "Crear cuenta"}
            </button>

            <button
              type="button"
              className="login-enlace"
              onClick={cambiarModo}
            >
              ¿Ya tienes una cuenta? Inicia sesión
            </button>
          </form>
        ) : (
          <form onSubmit={manejarLogin}>
            <div className="login-campo">
              <label>Usuario</label>

              <input
                type="text"
                value={usuario}
                onChange={(e) => setUsuario(e.target.value)}
                placeholder="Ingrese su usuario"
                autoComplete="username"
              />
            </div>

            <div className="login-campo">
              <label>Contraseña</label>

              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Ingrese su contraseña"
                autoComplete="current-password"
              />
            </div>

            {error && <div className="login-error">{error}</div>}

            {mensaje && <div className="login-mensaje">{mensaje}</div>}

            <button type="submit" className="login-boton" disabled={cargando}>
              {cargando ? "Iniciando sesión..." : "Iniciar sesión"}
            </button>
            <div className="login-separador">
              <span>o</span>
            </div>

            <button
              type="button"
              className="login-boton-microsoft"
              onClick={manejarLoginMicrosoft}
              disabled={cargando}
            >
              Continuar con Microsoft
            </button>
            <button
              type="button"
              className="login-enlace"
              onClick={cambiarModo}
            >
              ¿No tienes una cuenta? Regístrate
            </button>
          </form>
        )}
      </div>
    </div>
  );
}

export default Login;
