import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import App from "./App";
import {
  obtenerDashboard,
  obtenerPerfil,
  obtenerEstadoActualizacion,
} from "./services/api";

vi.mock("./services/api", () => ({
  obtenerDashboard: vi.fn(),
  obtenerPerfil: vi.fn(),
  obtenerEstadoActualizacion: vi.fn(),
  actualizarDatos: vi.fn(),
  iniciarSesion: vi.fn(),
  registrarUsuario: vi.fn(),
  iniciarSesionMicrosoft: vi.fn(),
  iniciarSesionGoogle: vi.fn(),
  solicitarResetPassword: vi.fn(),
  restablecerPassword: vi.fn(),
  verificarEmail: vi.fn(),
  reenviarVerificacion: vi.fn(),
}));

vi.mock("@azure/msal-react", () => ({
  useMsal: () => ({ instance: { loginRedirect: vi.fn() } }),
}));

vi.mock("@react-oauth/google", () => ({
  useGoogleLogin: () => vi.fn(),
}));

const dashboardVacio = { meses: [] };

const perfilUsuario = {
  id: 1,
  usuario: "jperez",
  nombre: "Juan Pérez",
  rol: "usuario",
  activo: true,
};

const perfilPendiente = {
  id: 2,
  usuario: "nuevo1",
  nombre: "Usuario Nuevo",
  rol: "pendiente",
  activo: true,
};

const estadoActualizacionInactivo = {
  ejecutando: false,
  estado: "Sin actualizaciones en curso.",
  progreso: 0,
  ultimaActualizacion: null,
};

afterEach(() => {
  vi.clearAllMocks();
  sessionStorage.clear();
});

describe("App", () => {
  it("sin token guardado, muestra el login", () => {
    render(<App />);

    expect(
      screen.getByRole("button", { name: "Iniciar sesión" }),
    ).toBeInTheDocument();
    expect(obtenerPerfil).not.toHaveBeenCalled();
  });

  it("con token guardado y rol 'pendiente', muestra la pantalla de cuenta pendiente", async () => {
    sessionStorage.setItem("token", "un-token-cualquiera");
    obtenerPerfil.mockResolvedValue(perfilPendiente);

    render(<App />);

    expect(
      await screen.findByText("Cuenta pendiente de aprobación"),
    ).toBeInTheDocument();
    expect(obtenerDashboard).not.toHaveBeenCalled();
  });

  it("con token guardado y rol 'usuario', muestra la aplicación completa", async () => {
    sessionStorage.setItem("token", "un-token-cualquiera");
    obtenerPerfil.mockResolvedValue(perfilUsuario);
    obtenerDashboard.mockResolvedValue(dashboardVacio);
    obtenerEstadoActualizacion.mockResolvedValue(estadoActualizacionInactivo);

    render(<App />);

    expect(
      await screen.findByText("Analítica de ventas"),
    ).toBeInTheDocument();
    expect(
      screen.queryByText("Cuenta pendiente de aprobación"),
    ).not.toBeInTheDocument();
    expect(obtenerDashboard).toHaveBeenCalled();
  });

  it(
    "mientras el usuario está pendiente, si lo aprueban se actualiza sola a la app completa",
    async () => {
      sessionStorage.setItem("token", "un-token-cualquiera");
      obtenerPerfil.mockResolvedValue(perfilPendiente);
      obtenerDashboard.mockResolvedValue(dashboardVacio);
      obtenerEstadoActualizacion.mockResolvedValue(estadoActualizacionInactivo);

      render(<App />);

      await screen.findByText("Cuenta pendiente de aprobación");

      obtenerPerfil.mockResolvedValue(perfilUsuario);

      expect(
        await screen.findByText(
          "Analítica de ventas",
          {},
          { timeout: 7000 },
        ),
      ).toBeInTheDocument();
    },
    8000,
  );

  it("al cerrar sesión desde la pantalla de cuenta pendiente, vuelve al login", async () => {
    sessionStorage.setItem("token", "un-token-cualquiera");
    obtenerPerfil.mockResolvedValue(perfilPendiente);
    const usuario = userEvent.setup();

    render(<App />);
    await screen.findByText("Cuenta pendiente de aprobación");

    await usuario.click(screen.getByRole("button", { name: "Cerrar sesión" }));

    expect(
      screen.getByRole("button", { name: "Iniciar sesión" }),
    ).toBeInTheDocument();
    expect(sessionStorage.getItem("token")).toBeNull();
  });
});
