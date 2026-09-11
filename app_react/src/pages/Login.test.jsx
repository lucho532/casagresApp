import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import Login from "./Login";
import {
  iniciarSesion,
  iniciarSesionMicrosoft,
  iniciarSesionGoogle,
  registrarUsuario,
  solicitarResetPassword,
  restablecerPassword,
  verificarEmail,
  reenviarVerificacion,
} from "../services/api";

vi.mock("../services/api", () => ({
  iniciarSesion: vi.fn(),
  registrarUsuario: vi.fn(),
  iniciarSesionMicrosoft: vi.fn(),
  iniciarSesionGoogle: vi.fn(),
  solicitarResetPassword: vi.fn(),
  restablecerPassword: vi.fn(),
  verificarEmail: vi.fn(),
  reenviarVerificacion: vi.fn(),
}));

const { loginRedirect } = vi.hoisted(() => ({ loginRedirect: vi.fn() }));

vi.mock("@azure/msal-react", () => ({
  useMsal: () => ({ instance: { loginRedirect } }),
}));

const { googleLoginTrigger, configGoogleLogin } = vi.hoisted(() => ({
  googleLoginTrigger: vi.fn(),
  configGoogleLogin: { current: null },
}));

vi.mock("@react-oauth/google", () => ({
  useGoogleLogin: (config) => {
    configGoogleLogin.current = config;
    return googleLoginTrigger;
  },
}));

afterEach(() => {
  vi.clearAllMocks();
  localStorage.clear();
  sessionStorage.clear();
  window.history.pushState({}, "", "/");
});

describe("Login", () => {
  it("muestra el formulario de inicio de sesión por defecto", () => {
    render(<Login iniciarSesionCorrectamente={vi.fn()} />);

    expect(screen.getByRole("button", { name: "Iniciar sesión" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Continuar con Microsoft" })).toBeInTheDocument();
  });

  it("valida que correo y contraseña sean obligatorios", async () => {
    const usuario = userEvent.setup();

    render(<Login iniciarSesionCorrectamente={vi.fn()} />);

    await usuario.click(screen.getByRole("button", { name: "Iniciar sesión" }));

    expect(
      await screen.findByText("Correo electrónico y contraseña son obligatorios."),
    ).toBeInTheDocument();
    expect(iniciarSesion).not.toHaveBeenCalled();
  });

  it("al iniciar sesión correctamente, guarda el token y notifica al padre", async () => {
    iniciarSesion.mockResolvedValue({ token: "un-token-jwt" });
    const iniciarSesionCorrectamente = vi.fn();
    const usuario = userEvent.setup();

    render(<Login iniciarSesionCorrectamente={iniciarSesionCorrectamente} />);

    await usuario.type(screen.getByPlaceholderText("Ingrese su correo electrónico"), "jperez@ejemplo.com");
    await usuario.type(screen.getByPlaceholderText("Ingrese su contraseña"), "clave123");
    await usuario.click(screen.getByRole("button", { name: "Iniciar sesión" }));

    await vi.waitFor(() =>
      expect(iniciarSesionCorrectamente).toHaveBeenCalledTimes(1),
    );

    expect(iniciarSesion).toHaveBeenCalledWith("jperez@ejemplo.com", "clave123");
    expect(sessionStorage.getItem("token")).toBe("un-token-jwt");
  });

  it("muestra 'credenciales incorrectas' cuando el backend responde 401", async () => {
    iniciarSesion.mockRejectedValue({ response: { status: 401 } });
    const usuario = userEvent.setup();

    render(<Login iniciarSesionCorrectamente={vi.fn()} />);

    await usuario.type(screen.getByPlaceholderText("Ingrese su correo electrónico"), "jperez@ejemplo.com");
    await usuario.type(screen.getByPlaceholderText("Ingrese su contraseña"), "mala-clave");
    await usuario.click(screen.getByRole("button", { name: "Iniciar sesión" }));

    expect(
      await screen.findByText("Correo electrónico o contraseña incorrectos."),
    ).toBeInTheDocument();
  });

  it("muestra un error genérico cuando falla la conexión", async () => {
    iniciarSesion.mockRejectedValue(new Error("network error"));
    const usuario = userEvent.setup();

    render(<Login iniciarSesionCorrectamente={vi.fn()} />);

    await usuario.type(screen.getByPlaceholderText("Ingrese su correo electrónico"), "jperez@ejemplo.com");
    await usuario.type(screen.getByPlaceholderText("Ingrese su contraseña"), "clave123");
    await usuario.click(screen.getByRole("button", { name: "Iniciar sesión" }));

    expect(
      await screen.findByText("No fue posible conectar con el servidor."),
    ).toBeInTheDocument();
  });

  it("cuando el backend rechaza el login por correo no verificado, ofrece reenviar la verificación", async () => {
    iniciarSesion.mockRejectedValue({
      response: {
        data: {
          codigo: "EMAIL_NO_VERIFICADO",
          mensaje: "Debes verificar tu correo electrónico antes de iniciar sesión.",
          email: "juan@ejemplo.com",
        },
      },
    });
    const usuario = userEvent.setup();

    render(<Login iniciarSesionCorrectamente={vi.fn()} />);

    await usuario.type(screen.getByPlaceholderText("Ingrese su correo electrónico"), "jperez@ejemplo.com");
    await usuario.type(screen.getByPlaceholderText("Ingrese su contraseña"), "clave123");
    await usuario.click(screen.getByRole("button", { name: "Iniciar sesión" }));

    expect(
      await screen.findByText("Debes verificar tu correo electrónico antes de iniciar sesión."),
    ).toBeInTheDocument();

    reenviarVerificacion.mockResolvedValue({
      mensaje: "Si el correo está registrado y pendiente de verificar, recibirás un nuevo enlace.",
    });

    await usuario.click(
      screen.getByRole("button", { name: "Reenviar correo de verificación" }),
    );

    expect(reenviarVerificacion).toHaveBeenCalledWith("juan@ejemplo.com");
    expect(
      await screen.findByText(
        "Si el correo está registrado y pendiente de verificar, recibirás un nuevo enlace.",
      ),
    ).toBeInTheDocument();
  });

  it("al hacer clic en 'Continuar con Microsoft', redirige con MSAL", async () => {
    const usuario = userEvent.setup();

    render(<Login iniciarSesionCorrectamente={vi.fn()} />);

    await usuario.click(screen.getByRole("button", { name: "Continuar con Microsoft" }));

    expect(loginRedirect).toHaveBeenCalledWith({ scopes: ["User.Read"] });
  });

  // ============================================================
  // LOGIN CON GOOGLE
  // ============================================================

  it("al hacer clic en 'Continuar con Google', inicia el flujo de OAuth de Google", async () => {
    const usuario = userEvent.setup();

    render(<Login iniciarSesionCorrectamente={vi.fn()} />);

    await usuario.click(
      screen.getByRole("button", { name: "Continuar con Google" }),
    );

    expect(googleLoginTrigger).toHaveBeenCalledTimes(1);
  });

  it("al iniciar sesión con Google correctamente, guarda el token y notifica al padre", async () => {
    iniciarSesionGoogle.mockResolvedValue({ token: "token-google" });
    const iniciarSesionCorrectamente = vi.fn();

    render(<Login iniciarSesionCorrectamente={iniciarSesionCorrectamente} />);

    await configGoogleLogin.current.onSuccess({
      access_token: "google-access-token",
    });

    await vi.waitFor(() =>
      expect(iniciarSesionCorrectamente).toHaveBeenCalledTimes(1),
    );

    expect(iniciarSesionGoogle).toHaveBeenCalledWith("google-access-token");
    expect(sessionStorage.getItem("token")).toBe("token-google");
  });

  it("muestra un error cuando el backend rechaza el login con Google", async () => {
    iniciarSesionGoogle.mockRejectedValue(new Error("token inválido"));

    render(<Login iniciarSesionCorrectamente={vi.fn()} />);

    await configGoogleLogin.current.onSuccess({
      access_token: "google-access-token",
    });

    expect(
      await screen.findByText("No fue posible iniciar sesión con Google."),
    ).toBeInTheDocument();
  });

  it("muestra un error cuando Google no puede completar el login", async () => {
    render(<Login iniciarSesionCorrectamente={vi.fn()} />);

    configGoogleLogin.current.onError();

    expect(
      await screen.findByText("No fue posible iniciar sesión con Google."),
    ).toBeInTheDocument();
    expect(iniciarSesionGoogle).not.toHaveBeenCalled();
  });

  // ============================================================
  // RECUPERAR CONTRASEÑA (solicitud)
  // ============================================================

  it("al hacer clic en '¿Olvidaste tu contraseña?', muestra el formulario de recuperación", async () => {
    const usuario = userEvent.setup();

    render(<Login iniciarSesionCorrectamente={vi.fn()} />);

    await usuario.click(
      screen.getByRole("button", { name: "¿Olvidaste tu contraseña?" }),
    );

    expect(
      screen.getByRole("button", { name: "Enviar enlace de recuperación" }),
    ).toBeInTheDocument();
  });

  it("valida que el correo sea obligatorio para solicitar el restablecimiento", async () => {
    const usuario = userEvent.setup();

    render(<Login iniciarSesionCorrectamente={vi.fn()} />);

    await usuario.click(
      screen.getByRole("button", { name: "¿Olvidaste tu contraseña?" }),
    );
    await usuario.click(
      screen.getByRole("button", { name: "Enviar enlace de recuperación" }),
    );

    expect(
      await screen.findByText("Ingresa tu correo electrónico."),
    ).toBeInTheDocument();
    expect(solicitarResetPassword).not.toHaveBeenCalled();
  });

  it("al solicitar el restablecimiento, muestra el mensaje de confirmación del backend", async () => {
    solicitarResetPassword.mockResolvedValue({
      mensaje: "Si el correo está registrado, recibirás un enlace para restablecer tu contraseña.",
    });
    const usuario = userEvent.setup();

    render(<Login iniciarSesionCorrectamente={vi.fn()} />);

    await usuario.click(
      screen.getByRole("button", { name: "¿Olvidaste tu contraseña?" }),
    );
    await usuario.type(
      screen.getByPlaceholderText("Ingrese su correo electrónico"),
      "juan@ejemplo.com",
    );
    await usuario.click(
      screen.getByRole("button", { name: "Enviar enlace de recuperación" }),
    );

    expect(
      await screen.findByText(
        "Si el correo está registrado, recibirás un enlace para restablecer tu contraseña.",
      ),
    ).toBeInTheDocument();
    expect(solicitarResetPassword).toHaveBeenCalledWith("juan@ejemplo.com");
  });

  it("muestra un error genérico cuando falla la solicitud de restablecimiento", async () => {
    solicitarResetPassword.mockRejectedValue(new Error("network error"));
    const usuario = userEvent.setup();

    render(<Login iniciarSesionCorrectamente={vi.fn()} />);

    await usuario.click(
      screen.getByRole("button", { name: "¿Olvidaste tu contraseña?" }),
    );
    await usuario.type(
      screen.getByPlaceholderText("Ingrese su correo electrónico"),
      "juan@ejemplo.com",
    );
    await usuario.click(
      screen.getByRole("button", { name: "Enviar enlace de recuperación" }),
    );

    expect(
      await screen.findByText("No fue posible procesar la solicitud."),
    ).toBeInTheDocument();
  });

  it("desde el formulario de recuperación, puede volver al login", async () => {
    const usuario = userEvent.setup();

    render(<Login iniciarSesionCorrectamente={vi.fn()} />);

    await usuario.click(
      screen.getByRole("button", { name: "¿Olvidaste tu contraseña?" }),
    );
    await usuario.click(
      screen.getByRole("button", { name: "¿Ya recordaste tu contraseña? Inicia sesión" }),
    );

    expect(screen.getByRole("button", { name: "Iniciar sesión" })).toBeInTheDocument();
  });

  // ============================================================
  // RESTABLECER CONTRASEÑA (con token en la URL)
  // ============================================================

  it("cuando la URL trae un token, muestra el formulario para definir una nueva contraseña", () => {
    window.history.pushState({}, "", "/?token=token-de-prueba");

    render(<Login iniciarSesionCorrectamente={vi.fn()} />);

    expect(
      screen.getByRole("button", { name: "Guardar nueva contraseña" }),
    ).toBeInTheDocument();
  });

  it("valida que ambos campos de contraseña sean obligatorios al restablecer", async () => {
    window.history.pushState({}, "", "/?token=token-de-prueba");
    const usuario = userEvent.setup();

    render(<Login iniciarSesionCorrectamente={vi.fn()} />);

    await usuario.click(
      screen.getByRole("button", { name: "Guardar nueva contraseña" }),
    );

    expect(
      await screen.findByText("Debes completar ambos campos de contraseña."),
    ).toBeInTheDocument();
    expect(restablecerPassword).not.toHaveBeenCalled();
  });

  it("valida la longitud mínima de la nueva contraseña", async () => {
    window.history.pushState({}, "", "/?token=token-de-prueba");
    const usuario = userEvent.setup();

    render(<Login iniciarSesionCorrectamente={vi.fn()} />);

    await usuario.type(
      screen.getByPlaceholderText("Ingrese su nueva contraseña"),
      "123",
    );
    await usuario.type(
      screen.getByPlaceholderText("Confirme su nueva contraseña"),
      "123",
    );
    await usuario.click(
      screen.getByRole("button", { name: "Guardar nueva contraseña" }),
    );

    expect(
      await screen.findByText("La contraseña debe tener al menos 6 caracteres."),
    ).toBeInTheDocument();
    expect(restablecerPassword).not.toHaveBeenCalled();
  });

  it("valida que las contraseñas coincidan al restablecer", async () => {
    window.history.pushState({}, "", "/?token=token-de-prueba");
    const usuario = userEvent.setup();

    render(<Login iniciarSesionCorrectamente={vi.fn()} />);

    await usuario.type(
      screen.getByPlaceholderText("Ingrese su nueva contraseña"),
      "clave123",
    );
    await usuario.type(
      screen.getByPlaceholderText("Confirme su nueva contraseña"),
      "otra-clave",
    );
    await usuario.click(
      screen.getByRole("button", { name: "Guardar nueva contraseña" }),
    );

    expect(
      await screen.findByText("Las contraseñas no coinciden."),
    ).toBeInTheDocument();
    expect(restablecerPassword).not.toHaveBeenCalled();
  });

  it("al restablecer correctamente, muestra confirmación y permite volver al login", async () => {
    window.history.pushState({}, "", "/?token=token-de-prueba");
    restablecerPassword.mockResolvedValue({ mensaje: "Contraseña actualizada correctamente." });
    const usuario = userEvent.setup();

    render(<Login iniciarSesionCorrectamente={vi.fn()} />);

    await usuario.type(
      screen.getByPlaceholderText("Ingrese su nueva contraseña"),
      "clave-nueva",
    );
    await usuario.type(
      screen.getByPlaceholderText("Confirme su nueva contraseña"),
      "clave-nueva",
    );
    await usuario.click(
      screen.getByRole("button", { name: "Guardar nueva contraseña" }),
    );

    expect(
      await screen.findByText("Contraseña actualizada correctamente. Ya puedes iniciar sesión."),
    ).toBeInTheDocument();
    expect(restablecerPassword).toHaveBeenCalledWith("token-de-prueba", "clave-nueva");

    await usuario.click(screen.getByRole("button", { name: "Ir a iniciar sesión" }));

    expect(screen.getByRole("button", { name: "Iniciar sesión" })).toBeInTheDocument();
  });

  it("muestra un error cuando el token es inválido o expiró", async () => {
    window.history.pushState({}, "", "/?token=token-invalido");
    restablecerPassword.mockRejectedValue({
      response: { data: { mensaje: "El enlace no es válido o ya expiró." } },
    });
    const usuario = userEvent.setup();

    render(<Login iniciarSesionCorrectamente={vi.fn()} />);

    await usuario.type(
      screen.getByPlaceholderText("Ingrese su nueva contraseña"),
      "clave-nueva",
    );
    await usuario.type(
      screen.getByPlaceholderText("Confirme su nueva contraseña"),
      "clave-nueva",
    );
    await usuario.click(
      screen.getByRole("button", { name: "Guardar nueva contraseña" }),
    );

    expect(
      await screen.findByText("El enlace no es válido o ya expiró."),
    ).toBeInTheDocument();
  });

  // ============================================================
  // VERIFICACIÓN DE CORREO (con enlace del correo)
  // ============================================================

  it("cuando la URL trae un token de verificación, lo verifica automáticamente y muestra éxito", async () => {
    window.history.pushState({}, "", "/?verificarEmail=token-de-verificacion");
    verificarEmail.mockResolvedValue({
      mensaje: "Correo verificado correctamente. Ya puedes iniciar sesión.",
    });

    render(<Login iniciarSesionCorrectamente={vi.fn()} />);

    expect(screen.getByText("Verificando tu correo...")).toBeInTheDocument();

    expect(
      await screen.findByText("Correo verificado correctamente. Ya puedes iniciar sesión."),
    ).toBeInTheDocument();
    expect(verificarEmail).toHaveBeenCalledWith("token-de-verificacion");

    await userEvent.setup().click(
      screen.getByRole("button", { name: "Ir a iniciar sesión" }),
    );

    expect(screen.getByRole("button", { name: "Iniciar sesión" })).toBeInTheDocument();
  });

  it("cuando el token de verificación es inválido o expiró, muestra un error", async () => {
    window.history.pushState({}, "", "/?verificarEmail=token-invalido");
    verificarEmail.mockRejectedValue({
      response: { data: { mensaje: "El enlace de verificación no es válido o ya expiró." } },
    });

    render(<Login iniciarSesionCorrectamente={vi.fn()} />);

    expect(
      await screen.findByText("El enlace de verificación no es válido o ya expiró."),
    ).toBeInTheDocument();
  });

  // ============================================================
  // REGISTRO
  // ============================================================

  it("cambia al formulario de registro y de vuelta al de inicio de sesión", async () => {
    const usuario = userEvent.setup();

    render(<Login iniciarSesionCorrectamente={vi.fn()} />);

    await usuario.click(
      screen.getByRole("button", { name: "¿No tienes una cuenta? Regístrate" }),
    );
    expect(screen.getByRole("button", { name: "Crear cuenta" })).toBeInTheDocument();

    await usuario.click(
      screen.getByRole("button", { name: "¿Ya tienes una cuenta? Inicia sesión" }),
    );
    expect(screen.getByRole("button", { name: "Iniciar sesión" })).toBeInTheDocument();
  });

  it("valida que todos los campos de registro sean obligatorios", async () => {
    const usuario = userEvent.setup();

    render(<Login iniciarSesionCorrectamente={vi.fn()} />);
    await usuario.click(
      screen.getByRole("button", { name: "¿No tienes una cuenta? Regístrate" }),
    );

    await usuario.click(screen.getByRole("button", { name: "Crear cuenta" }));

    expect(
      await screen.findByText(
        "Nombre, correo electrónico y contraseña son obligatorios.",
      ),
    ).toBeInTheDocument();
    expect(registrarUsuario).not.toHaveBeenCalled();
  });

  it("al registrarse correctamente, muestra confirmación y vuelve al login", async () => {
    registrarUsuario.mockResolvedValue({});
    const usuario = userEvent.setup();

    render(<Login iniciarSesionCorrectamente={vi.fn()} />);
    await usuario.click(
      screen.getByRole("button", { name: "¿No tienes una cuenta? Regístrate" }),
    );

    await usuario.type(screen.getByPlaceholderText("Ingrese su nombre"), "Juan Pérez");
    await usuario.type(
      screen.getByPlaceholderText("Ingrese su correo electrónico"),
      "juan@ejemplo.com",
    );
    await usuario.type(screen.getByPlaceholderText("Ingrese su contraseña"), "clave123");
    await usuario.click(screen.getByRole("button", { name: "Crear cuenta" }));

    expect(
      await screen.findByText(
        "Usuario creado correctamente. Revisa tu correo para verificar tu cuenta antes de iniciar sesión.",
      ),
    ).toBeInTheDocument();
    expect(registrarUsuario).toHaveBeenCalledWith(
      "clave123",
      "Juan Pérez",
      "juan@ejemplo.com",
    );

    // Volvió a mostrar el formulario de inicio de sesión.
    expect(screen.getByRole("button", { name: "Iniciar sesión" })).toBeInTheDocument();
  });

  it("muestra error cuando el correo ya existe (409)", async () => {
    registrarUsuario.mockRejectedValue({ response: { status: 409 } });
    const usuario = userEvent.setup();

    render(<Login iniciarSesionCorrectamente={vi.fn()} />);
    await usuario.click(
      screen.getByRole("button", { name: "¿No tienes una cuenta? Regístrate" }),
    );

    await usuario.type(screen.getByPlaceholderText("Ingrese su nombre"), "Juan Pérez");
    await usuario.type(
      screen.getByPlaceholderText("Ingrese su correo electrónico"),
      "juan@ejemplo.com",
    );
    await usuario.type(screen.getByPlaceholderText("Ingrese su contraseña"), "clave123");
    await usuario.click(screen.getByRole("button", { name: "Crear cuenta" }));

    expect(
      await screen.findByText("Ese correo electrónico ya está registrado."),
    ).toBeInTheDocument();
  });

  // ============================================================
  // LOGIN CON MICROSOFT AL MONTAR (redirect flow)
  // ============================================================

  it("si no hay id_token de Microsoft pendiente, no hace nada al montar", () => {
    render(<Login iniciarSesionCorrectamente={vi.fn()} />);

    expect(iniciarSesionMicrosoft).not.toHaveBeenCalled();
  });

  it("al montar con un id_token pendiente, completa el login con Microsoft", async () => {
    sessionStorage.setItem("microsoft_id_token", "id-token-de-microsoft");
    iniciarSesionMicrosoft.mockResolvedValue({ token: "token-casagres" });
    const iniciarSesionCorrectamente = vi.fn();

    render(<Login iniciarSesionCorrectamente={iniciarSesionCorrectamente} />);

    await vi.waitFor(() =>
      expect(iniciarSesionCorrectamente).toHaveBeenCalledTimes(1),
    );

    expect(iniciarSesionMicrosoft).toHaveBeenCalledWith("id-token-de-microsoft");
    expect(sessionStorage.getItem("token")).toBe("token-casagres");
    expect(sessionStorage.getItem("microsoft_id_token")).toBeNull();
  });

  it("si falla el login con Microsoft al montar, muestra un error y limpia el id_token", async () => {
    sessionStorage.setItem("microsoft_id_token", "id-token-de-microsoft");
    iniciarSesionMicrosoft.mockRejectedValue(new Error("token inválido"));

    render(<Login iniciarSesionCorrectamente={vi.fn()} />);

    expect(
      await screen.findByText("No fue posible iniciar sesión con Microsoft."),
    ).toBeInTheDocument();
    expect(sessionStorage.getItem("microsoft_id_token")).toBeNull();
  });
});
