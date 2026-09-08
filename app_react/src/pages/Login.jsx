/*
=========================================================
 LOGIN - CASAGRES
=========================================================

Este componente se encarga de gestionar la pantalla de
autenticación y registro de usuarios de la plataforma
CASAGRES.

FUNCIONALIDADES PRINCIPALES:

1. INICIO DE SESIÓN
   Permite al usuario introducir su usuario y contraseña.

   Los datos se envían al backend mediante la función
   iniciarSesion() del servicio API.

   Si las credenciales son correctas, el backend devuelve
   un JWT que se almacena en localStorage.

   Después de guardar el token, se informa al componente
   principal (App.jsx) de que el usuario se ha autenticado
   correctamente.

2. REGISTRO DE USUARIOS
   Permite crear una nueva cuenta introduciendo:

   - Nombre
   - Correo electrónico
   - Usuario
   - Contraseña

   Los datos se envían al backend mediante registrarUsuario().

   Si el registro es correcto, se muestra un mensaje de
   confirmación y el usuario vuelve automáticamente al
   formulario de inicio de sesión.

3. CAMBIO ENTRE LOGIN Y REGISTRO
   El usuario puede cambiar entre los dos modos mediante
   los enlaces situados debajo de cada formulario.

   Al cambiar de modo se limpian los campos y los mensajes
   anteriores.

4. GESTIÓN DE ESTADOS
   El componente controla diferentes estados de la interfaz:

   - modoRegistro:
     Determina si se muestra el formulario de registro o login.

   - usuario:
     Guarda el usuario introducido.

   - password:
     Guarda la contraseña introducida.

   - nombre:
     Guarda el nombre durante el registro.

   - email:
     Guarda el correo electrónico durante el registro.

   - cargando:
     Indica si se está realizando una petición al backend.

   - error:
     Contiene los mensajes de error que se muestran al usuario.

   - mensaje:
     Contiene mensajes informativos o de confirmación.

5. MANEJO DE ERRORES
   Los errores devueltos por la API se analizan según su
   código HTTP.

   Por ejemplo:

   - 401 → Usuario o contraseña incorrectos.
   - 409 → El usuario o correo ya existe.
   - Otros errores → Problema de conexión o del servidor.

IMPORTANTE:

Este componente NO valida directamente las credenciales
contra la base de datos.

La autenticación real se realiza en el backend mediante
la API de CASAGRES.

El frontend únicamente:

   React
     ↓
   Envía credenciales
     ↓
   API CASAGRES
     ↓
   Valida usuario
     ↓
   Genera JWT
     ↓
   React guarda el JWT
     ↓
   Usuario autenticado

En futuras versiones este componente también podrá gestionar
otros métodos de autenticación, como OAuth con Microsoft o
Google, además del proceso de recuperación de contraseña.

=========================================================
*/

import { useEffect, useState } from "react";
import {
  iniciarSesion,
  registrarUsuario,
  iniciarSesionMicrosoft,
} from "../services/api";
import { useMsal } from "@azure/msal-react";
import { loginRequest } from "../authConfig";
import "../styles/Login.css";

console.log("LOGIN.JSX CARGADO");
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
        console.log("Procesando autenticación de Microsoft...");

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
      console.log("1. Iniciando login Microsoft");

      setCargando(true);
      setError("");
      setMensaje("");

      console.log("2. Redirigiendo a Microsoft");

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
