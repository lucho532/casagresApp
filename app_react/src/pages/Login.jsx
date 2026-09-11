// Pantalla de autenticación y registro. La validación real de
// credenciales ocurre en el backend; aquí solo se gestiona el
// formulario y se persiste el JWT que devuelve la API.

import { useEffect, useState } from "react";
import {
  iniciarSesion,
  registrarUsuario,
  iniciarSesionMicrosoft,
  iniciarSesionGoogle,
  solicitarResetPassword,
  restablecerPassword,
  verificarEmail,
  reenviarVerificacion,
} from "../services/api";
import { useMsal } from "@azure/msal-react";
import { useGoogleLogin } from "@react-oauth/google";
import { loginRequest } from "../authConfig";
import "../styles/Login.css";

// Ladrillos decorativos que caen de fondo; el número de elementos define
// la densidad de la lluvia (cada uno se posiciona y anima por CSS).
const LADRILLOS_DECORATIVOS = Array.from({ length: 14 });

function Login({ iniciarSesionCorrectamente }) {
  const [modoRegistro, setModoRegistro] = useState(false);
  const [modoRecuperar, setModoRecuperar] = useState(false);

  const [password, setPassword] = useState("");
  const [nombre, setNombre] = useState("");
  const [email, setEmail] = useState("");

  const [emailRecuperacion, setEmailRecuperacion] = useState("");
  const [nuevaPassword, setNuevaPassword] = useState("");
  const [confirmarPassword, setConfirmarPassword] = useState("");

  // Si el usuario llegó desde el enlace de un correo de recuperación,
  // la URL trae ?token=... y hay que mostrar el formulario para definir
  // una contraseña nueva en vez del login normal.
  const [tokenReset, setTokenReset] = useState(
    () => new URLSearchParams(window.location.search).get("token"),
  );

  // Si llegó desde el enlace de confirmación de correo (?verificarEmail=...),
  // se verifica automáticamente al montar, sin pedir ninguna acción extra.
  const [tokenVerificacion, setTokenVerificacion] = useState(
    () => new URLSearchParams(window.location.search).get("verificarEmail"),
  );
  const [verificandoEmail, setVerificandoEmail] = useState(!!tokenVerificacion);

  // Correo asociado a un intento de login rechazado por no estar
  // verificado, para poder ofrecer reenviar el correo de confirmación.
  const [emailNoVerificado, setEmailNoVerificado] = useState("");

  const [cargando, setCargando] = useState(false);
  const [error, setError] = useState("");
  const [mensaje, setMensaje] = useState("");

  // Si venimos de un redirect de Microsoft, hay un id_token pendiente de
  // canjear por el token de CASAGRES. Mientras eso ocurre no debe mostrarse
  // el formulario, o parecerá que la sesión no se inició y hay que volver
  // a ingresar los datos.
  const [procesandoMicrosoft, setProcesandoMicrosoft] = useState(
    () => !!sessionStorage.getItem("microsoft_id_token"),
  );

  const { instance } = useMsal();

  useEffect(() => {
    const procesarLoginMicrosoft = async () => {
      const idToken = sessionStorage.getItem("microsoft_id_token");

      if (!idToken) {
        setProcesandoMicrosoft(false);
        return;
      }

      try {
        setProcesandoMicrosoft(true);
        setError("");
        setMensaje("");

        const respuesta = await iniciarSesionMicrosoft(idToken);

        if (!respuesta.token) {
          throw new Error("El servidor no devolvió un token de CASAGRES.");
        }

        sessionStorage.setItem("token", respuesta.token);

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
        setProcesandoMicrosoft(false);
      }
    };

    procesarLoginMicrosoft();
  }, [iniciarSesionCorrectamente]);

  useEffect(() => {
    if (!tokenVerificacion) {
      return;
    }

    const verificar = async () => {
      try {
        const respuesta = await verificarEmail(tokenVerificacion);

        setMensaje(
          respuesta.mensaje ||
            "Correo verificado correctamente. Ya puedes iniciar sesión.",
        );
      } catch (err) {
        console.error("Error al verificar el correo:", err);

        setError(
          err.response?.data?.mensaje ||
            "El enlace de verificación no es válido o ya expiró.",
        );
      } finally {
        setVerificandoEmail(false);
      }
    };

    verificar();
  }, [tokenVerificacion]);

  const volverAlLoginDesdeVerificacion = () => {
    setTokenVerificacion(null);
    window.history.replaceState({}, "", window.location.pathname);

    setError("");
    setMensaje("");
  };

  const manejarLogin = async (e) => {
    e.preventDefault();

    setError("");
    setMensaje("");

    if (!email.trim() || !password.trim()) {
      setError("Correo electrónico y contraseña son obligatorios.");
      return;
    }

    setEmailNoVerificado("");

    try {
      setCargando(true);

      const respuesta = await iniciarSesion(email.trim(), password);

      sessionStorage.setItem("token", respuesta.token);

      iniciarSesionCorrectamente();
    } catch (err) {
      console.error(err);

      if (err.response?.data?.codigo === "EMAIL_NO_VERIFICADO") {
        setError(err.response.data.mensaje);
        setEmailNoVerificado(err.response.data.email || "");
      } else if (err.response?.status === 401) {
        setError("Correo electrónico o contraseña incorrectos.");
      } else {
        setError("No fue posible conectar con el servidor.");
      }
    } finally {
      setCargando(false);
    }
  };

  const manejarReenviarVerificacion = async () => {
    if (!emailNoVerificado) {
      return;
    }

    try {
      setCargando(true);

      const respuesta = await reenviarVerificacion(emailNoVerificado);

      setMensaje(
        respuesta.mensaje ||
          "Si el correo está registrado y pendiente de verificar, recibirás un nuevo enlace.",
      );
      setEmailNoVerificado("");
    } catch (err) {
      console.error("Error al reenviar la verificación:", err);

      setError("No fue posible reenviar el correo de verificación.");
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

  const manejarExitoGoogle = async (tokenResponse) => {
    try {
      setCargando(true);
      setError("");
      setMensaje("");

      const respuesta = await iniciarSesionGoogle(
        tokenResponse.access_token,
      );

      sessionStorage.setItem("token", respuesta.token);

      iniciarSesionCorrectamente();
    } catch (err) {
      console.error("Error al iniciar sesión con Google:", err);

      setError("No fue posible iniciar sesión con Google.");
    } finally {
      setCargando(false);
    }
  };

  const manejarErrorGoogle = () => {
    setError("No fue posible iniciar sesión con Google.");
  };

  const iniciarLoginGoogle = useGoogleLogin({
    onSuccess: manejarExitoGoogle,
    onError: manejarErrorGoogle,
    scope: "openid email profile",
  });

  const manejarLoginGoogle = () => {
    setError("");
    setMensaje("");

    iniciarLoginGoogle();
  };

  const manejarSolicitarReset = async (e) => {
    e.preventDefault();

    setError("");
    setMensaje("");

    if (!emailRecuperacion.trim()) {
      setError("Ingresa tu correo electrónico.");
      return;
    }

    try {
      setCargando(true);

      const respuesta = await solicitarResetPassword(emailRecuperacion.trim());

      setMensaje(
        respuesta.mensaje ||
          "Si el correo está registrado, recibirás un enlace para restablecer tu contraseña.",
      );
      setEmailRecuperacion("");
    } catch (err) {
      console.error("Error al solicitar el restablecimiento:", err);

      setError("No fue posible procesar la solicitud.");
    } finally {
      setCargando(false);
    }
  };

  const irARecuperar = () => {
    setModoRecuperar(true);
    setError("");
    setMensaje("");
  };

  const volverAlLogin = () => {
    setModoRecuperar(false);
    setError("");
    setMensaje("");
    setEmailRecuperacion("");
  };

  const manejarRestablecer = async (e) => {
    e.preventDefault();

    setError("");

    if (!nuevaPassword.trim() || !confirmarPassword.trim()) {
      setError("Debes completar ambos campos de contraseña.");
      return;
    }

    if (nuevaPassword.length < 6) {
      setError("La contraseña debe tener al menos 6 caracteres.");
      return;
    }

    if (nuevaPassword !== confirmarPassword) {
      setError("Las contraseñas no coinciden.");
      return;
    }

    try {
      setCargando(true);

      await restablecerPassword(tokenReset, nuevaPassword);

      setMensaje("Contraseña actualizada correctamente. Ya puedes iniciar sesión.");
    } catch (err) {
      console.error("Error al restablecer la contraseña:", err);

      setError(
        err.response?.data?.mensaje || "El enlace no es válido o ya expiró.",
      );
    } finally {
      setCargando(false);
    }
  };

  const volverAlLoginDesdeReset = () => {
    setTokenReset(null);
    window.history.replaceState({}, "", window.location.pathname);

    setError("");
    setMensaje("");
    setNuevaPassword("");
    setConfirmarPassword("");
  };

  const manejarRegistro = async (e) => {
    e.preventDefault();

    setError("");
    setMensaje("");

    if (
      !password.trim() ||
      !nombre.trim() ||
      !email.trim()
    ) {
      setError(
        "Nombre, correo electrónico y contraseña son obligatorios.",
      );
      return;
    }

    try {
      setCargando(true);

      const respuesta = await registrarUsuario(
        password,
        nombre.trim(),
        email.trim(),
      );

      setMensaje(
        respuesta.mensaje ||
          "Usuario creado correctamente. Revisa tu correo para verificar tu cuenta antes de iniciar sesión.",
      );

      setModoRegistro(false);

      setPassword("");
      setNombre("");
      setEmail("");
    } catch (err) {
      console.error(err);

      if (err.response?.status === 409) {
        setError("Ese correo electrónico ya está registrado.");
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

    setPassword("");
    setNombre("");
    setEmail("");
  };

  return (
    <div className="login-pagina">
      <div className="login-lluvia-ladrillos" aria-hidden="true">
        {LADRILLOS_DECORATIVOS.map((_, indice) => (
          <span key={indice} className="ladrillo" />
        ))}
      </div>

      <div className="login-tarjeta">
        <div className="login-logo">
          <div className="logo-c">C</div>
        </div>

        <h1>CASAGRES</h1>

        <p className="login-subtitulo">
          Plataforma de Analítica & Predicción de Demanda
        </p>

        {procesandoMicrosoft ? (
          <div className="estado-cargando">
            <div className="spinner" />
            <p>Verificando tu sesión de Microsoft...</p>
          </div>
        ) : tokenVerificacion ? (
          verificandoEmail ? (
            <div className="estado-cargando">
              <div className="spinner" />
              <p>Verificando tu correo...</p>
            </div>
          ) : (
            <div>
              {mensaje && <div className="login-mensaje">{mensaje}</div>}

              {error && <div className="login-error">{error}</div>}

              <button
                type="button"
                className="login-boton"
                onClick={volverAlLoginDesdeVerificacion}
              >
                Ir a iniciar sesión
              </button>
            </div>
          )
        ) : tokenReset ? (
          mensaje ? (
            <div>
              <div className="login-mensaje">{mensaje}</div>

              <button
                type="button"
                className="login-boton"
                onClick={volverAlLoginDesdeReset}
              >
                Ir a iniciar sesión
              </button>
            </div>
          ) : (
            <form onSubmit={manejarRestablecer}>
              <div className="login-campo">
                <label>Nueva contraseña</label>

                <input
                  type="password"
                  value={nuevaPassword}
                  onChange={(e) => setNuevaPassword(e.target.value)}
                  placeholder="Ingrese su nueva contraseña"
                  autoComplete="new-password"
                />
              </div>

              <div className="login-campo">
                <label>Confirmar contraseña</label>

                <input
                  type="password"
                  value={confirmarPassword}
                  onChange={(e) => setConfirmarPassword(e.target.value)}
                  placeholder="Confirme su nueva contraseña"
                  autoComplete="new-password"
                />
              </div>

              {error && <div className="login-error">{error}</div>}

              <button type="submit" className="login-boton" disabled={cargando}>
                {cargando ? "Guardando..." : "Guardar nueva contraseña"}
              </button>
            </form>
          )
        ) : modoRecuperar ? (
          <form onSubmit={manejarSolicitarReset}>
            <div className="login-campo">
              <label>Correo electrónico</label>

              <input
                type="email"
                value={emailRecuperacion}
                onChange={(e) => setEmailRecuperacion(e.target.value)}
                placeholder="Ingrese su correo electrónico"
                autoComplete="email"
              />
            </div>

            {error && <div className="login-error">{error}</div>}

            {mensaje && <div className="login-mensaje">{mensaje}</div>}

            <button type="submit" className="login-boton" disabled={cargando}>
              {cargando ? "Enviando..." : "Enviar enlace de recuperación"}
            </button>

            <button
              type="button"
              className="login-enlace"
              onClick={volverAlLogin}
            >
              ¿Ya recordaste tu contraseña? Inicia sesión
            </button>
          </form>
        ) : modoRegistro ? (
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
              <label>Contraseña</label>

              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Ingrese su contraseña"
                autoComplete="current-password"
              />
            </div>

            <button
              type="button"
              className="login-enlace login-enlace-olvido"
              onClick={irARecuperar}
            >
              ¿Olvidaste tu contraseña?
            </button>

            {error && <div className="login-error">{error}</div>}

            {emailNoVerificado && (
              <button
                type="button"
                className="login-enlace"
                onClick={manejarReenviarVerificacion}
                disabled={cargando}
              >
                Reenviar correo de verificación
              </button>
            )}

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
              className="login-boton-google"
              onClick={manejarLoginGoogle}
              disabled={cargando}
            >
              Continuar con Google
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
