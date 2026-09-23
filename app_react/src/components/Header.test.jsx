import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import Header from "./Header";
import { obtenerNombre, obtenerRol } from "../utils/auth";

vi.mock("../utils/auth", () => ({
  obtenerNombre: vi.fn(),
  obtenerRol: vi.fn(),
}));

beforeEach(() => {
  obtenerNombre.mockReturnValue(null);
  obtenerRol.mockReturnValue(null);
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

  it("muestra el ícono genérico cuando no hay foto de perfil", () => {
    render(<Header paginaActual="inicio" />);

    expect(screen.getByText("👤")).toBeInTheDocument();
    expect(screen.queryByAltText("Foto de perfil")).not.toBeInTheDocument();
  });

  it("muestra la foto de perfil cuando está disponible", () => {
    render(
      <Header
        paginaActual="inicio"
        fotoUrl="https://foto.ejemplo.com/perfil.jpg"
      />,
    );

    const imagen = screen.getByAltText("Foto de perfil");
    expect(imagen).toHaveAttribute("src", "https://foto.ejemplo.com/perfil.jpg");
    expect(screen.queryByText("👤")).not.toBeInTheDocument();
  });

  it("si la foto de perfil falla al cargar, vuelve a mostrar el ícono genérico", () => {
    render(
      <Header
        paginaActual="inicio"
        fotoUrl="https://foto.ejemplo.com/rota.jpg"
      />,
    );

    const imagen = screen.getByAltText("Foto de perfil");
    fireEvent.error(imagen);

    expect(screen.getByText("👤")).toBeInTheDocument();
    expect(screen.queryByAltText("Foto de perfil")).not.toBeInTheDocument();
  });
});
