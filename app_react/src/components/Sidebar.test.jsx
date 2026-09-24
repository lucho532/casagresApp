import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import Sidebar from "./Sidebar";
import { obtenerRol } from "../utils/auth";

vi.mock("../utils/auth", () => ({
  obtenerRol: vi.fn(),
}));

// ControlActualizacion tiene su propia lógica (y su propio archivo de
// pruebas); aquí se reemplaza por un stub para aislar a Sidebar.
vi.mock("./ControlActualizacion", () => ({
  default: () => <div data-testid="control-actualizacion-mock" />,
}));

beforeEach(() => {
  obtenerRol.mockReturnValue("usuario");
});

const propsBase = {
  paginaActual: "inicio",
  cambiarPagina: vi.fn(),
  cerrarSesion: vi.fn(),
  onUltimaActualizacion: vi.fn(),
  onActualizacionCompletada: vi.fn(),
};

describe("Sidebar", () => {
  it("muestra las opciones de menú base", () => {
    render(<Sidebar {...propsBase} />);

    expect(screen.getByText("Inicio")).toBeInTheDocument();
    expect(screen.getByText("Tendencias de compra")).toBeInTheDocument();
    expect(screen.getByText("Demanda futura")).toBeInTheDocument();
    expect(screen.getByText("Enfoque en decisiones")).toBeInTheDocument();
    expect(screen.getByText("Power BI")).toBeInTheDocument();
    expect(screen.getByText("Productos")).toBeInTheDocument();
  });

  it("no muestra 'Administración' cuando el usuario no es admin", () => {
    obtenerRol.mockReturnValue("usuario");

    render(<Sidebar {...propsBase} />);

    expect(screen.queryByText("Administración")).not.toBeInTheDocument();
  });

  it("muestra 'Administración' cuando el usuario es admin", () => {
    obtenerRol.mockReturnValue("admin");

    render(<Sidebar {...propsBase} />);

    expect(screen.getByText("Administración")).toBeInTheDocument();
  });

  it("resalta la opción de la página actual", () => {
    render(<Sidebar {...propsBase} paginaActual="productos" />);

    expect(screen.getByText("Productos").closest("button")).toHaveClass("activo");
    expect(screen.getByText("Inicio").closest("button")).not.toHaveClass("activo");
  });

  it("al hacer clic en una opción, llama a cambiarPagina con su id", async () => {
    const cambiarPagina = vi.fn();
    const usuario = userEvent.setup();

    render(<Sidebar {...propsBase} cambiarPagina={cambiarPagina} />);

    await usuario.click(screen.getByText("Demanda futura"));

    expect(cambiarPagina).toHaveBeenCalledWith("demanda");
  });

  it("al hacer clic en 'Cerrar sesión', llama a cerrarSesion", async () => {
    const cerrarSesion = vi.fn();
    const usuario = userEvent.setup();

    render(<Sidebar {...propsBase} cerrarSesion={cerrarSesion} />);

    await usuario.click(screen.getByText("Cerrar sesión"));

    expect(cerrarSesion).toHaveBeenCalledTimes(1);
  });

  it("incluye el control de actualización", () => {
    render(<Sidebar {...propsBase} />);

    expect(screen.getByTestId("control-actualizacion-mock")).toBeInTheDocument();
  });

  it("al hacer clic en el logo, llama a cambiarPagina con 'inicio'", async () => {
    const cambiarPagina = vi.fn();
    const usuario = userEvent.setup();

    render(
      <Sidebar {...propsBase} paginaActual="productos" cambiarPagina={cambiarPagina} />,
    );

    await usuario.click(screen.getByText("CASAGRES", { selector: ".logo-nombre" }));

    expect(cambiarPagina).toHaveBeenCalledWith("inicio");
  });
});
