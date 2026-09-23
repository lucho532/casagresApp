import { render, screen } from "@testing-library/react";
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
});
