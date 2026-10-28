import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import Header from "./Header";
import { obtenerNombre, obtenerRol } from "../utils/auth";
import { obtenerUrlCompleta } from "../services/api";

vi.mock("../utils/auth", () => ({
  obtenerNombre: vi.fn(),
  obtenerRol: vi.fn(),
}));

vi.mock("../services/api", () => ({
  obtenerUrlCompleta: vi.fn((ruta) => (ruta ? `https://api.ejemplo.com${ruta}` : ruta)),
}));

beforeEach(() => {
  obtenerNombre.mockReturnValue(null);
  obtenerRol.mockReturnValue(null);
  vi.clearAllMocks();
});

describe("Header", () => {
  it("muestra el título correspondiente a la página actual", () => {
    render(<Header paginaActual="tendencias" />);

    expect(
      screen.getByRole("heading", { name: "Tendencias de compra" }),
    ).toBeInTheDocument();
  });

  it("muestra 'CASAGRES' cuando la página no tiene título definido", () => {
    render(<Header paginaActual="pagina-desconocida" />);

    expect(screen.getByRole("heading", { name: "CASAGRES" })).toBeInTheDocument();
  });

  it("muestra 'Mi perfil' como título cuando la página actual es perfil", () => {
    render(<Header paginaActual="perfil" />);

    expect(screen.getByRole("heading", { name: "Mi perfil" })).toBeInTheDocument();
  });

  it("muestra el nombre del usuario cuando está disponible", () => {
    obtenerNombre.mockReturnValue("Juan Pérez");

    render(<Header paginaActual="inicio" />);

    expect(screen.getByText("Juan Pérez")).toBeInTheDocument();
  });

  it("muestra 'Usuario' cuando no hay nombre disponible", () => {
    render(<Header paginaActual="inicio" />);

    expect(
      screen.getByText("Usuario", { selector: ".header-usuario-nombre" }),
    ).toBeInTheDocument();
  });

  it("muestra 'Administrador' cuando el rol es admin", () => {
    obtenerRol.mockReturnValue("admin");

    render(<Header paginaActual="inicio" />);

    expect(screen.getByText("Administrador")).toBeInTheDocument();
  });

  it("muestra 'Usuario' como rol cuando no es admin", () => {
    obtenerRol.mockReturnValue("usuario");

    render(<Header paginaActual="inicio" />);

    expect(
      screen.getByText("Usuario", { selector: ".header-usuario-rol" }),
    ).toBeInTheDocument();
  });

  it("no muestra la última actualización cuando no se recibe fecha", () => {
    render(<Header paginaActual="inicio" ultimaActualizacion={null} />);

    expect(screen.queryByText(/Última actualización/)).not.toBeInTheDocument();
  });

  it("muestra la última actualización cuando la fecha es válida", () => {
    render(
      <Header paginaActual="inicio" ultimaActualizacion="2025-06-01T10:00:00Z" />,
    );

    expect(screen.getByText(/Última actualización:/)).toBeInTheDocument();
  });

  it("no muestra la última actualización cuando la fecha es inválida", () => {
    render(<Header paginaActual="inicio" ultimaActualizacion="fecha-no-valida" />);

    expect(screen.queryByText(/Última actualización/)).not.toBeInTheDocument();
  });

  it("al hacer clic en el nombre, llama a onAbrirPerfil", async () => {
    const onAbrirPerfil = vi.fn();
    obtenerNombre.mockReturnValue("Juan Pérez");
    const usuario = userEvent.setup();

    render(<Header paginaActual="inicio" onAbrirPerfil={onAbrirPerfil} />);

    await usuario.click(screen.getByRole("button", { name: "Juan Pérez" }));

    expect(onAbrirPerfil).toHaveBeenCalledTimes(1);
  });

  describe("avatar", () => {
    it("muestra las iniciales cuando no hay foto de perfil", () => {
      obtenerNombre.mockReturnValue("Juan Pérez");

      render(<Header paginaActual="inicio" />);

      expect(screen.getByText("JP")).toBeInTheDocument();
      expect(screen.queryByAltText("Foto de perfil")).not.toBeInTheDocument();
    });

    it("muestra el ícono genérico cuando no hay nombre ni foto", () => {
      render(<Header paginaActual="inicio" />);

      expect(screen.getByText("👤")).toBeInTheDocument();
    });

    it("muestra la foto de perfil cuando está disponible", () => {
      render(<Header paginaActual="inicio" fotoUrl="/api/auth/foto-perfil/1" />);

      const imagen = screen.getByAltText("Foto de perfil");
      expect(imagen).toHaveAttribute("src", "https://api.ejemplo.com/api/auth/foto-perfil/1");
      expect(obtenerUrlCompleta).toHaveBeenCalledWith("/api/auth/foto-perfil/1");
    });

    it("si la foto falla al cargar, vuelve a mostrar las iniciales", () => {
      obtenerNombre.mockReturnValue("Juan Pérez");

      render(<Header paginaActual="inicio" fotoUrl="/api/auth/foto-perfil/1" />);

      fireEvent.error(screen.getByAltText("Foto de perfil"));

      expect(screen.getByText("JP")).toBeInTheDocument();
      expect(screen.queryByAltText("Foto de perfil")).not.toBeInTheDocument();
    });
  });
});
