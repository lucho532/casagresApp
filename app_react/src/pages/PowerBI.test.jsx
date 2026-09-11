import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import PowerBI from "./PowerBI";
import {
  obtenerTablerosPowerBi,
  agregarTableroPowerBi,
  eliminarTableroPowerBi,
} from "../services/api";
import { obtenerRol } from "../utils/auth";

vi.mock("../services/api", () => ({
  obtenerTablerosPowerBi: vi.fn(),
  agregarTableroPowerBi: vi.fn(),
  eliminarTableroPowerBi: vi.fn(),
}));

vi.mock("../utils/auth", () => ({
  obtenerRol: vi.fn(),
}));

const tableros = [
  {
    id: 2,
    nombre: "Ventas 2026",
    url: "https://app.powerbi.com/view?r=nuevo",
    fechaCreacion: "2026-06-01T00:00:00Z",
  },
  {
    id: 1,
    nombre: "Ventas por los últimos años",
    url: "https://app.powerbi.com/view?r=viejo",
    fechaCreacion: "2025-01-01T00:00:00Z",
  },
];

afterEach(() => {
  vi.clearAllMocks();
});

describe("PowerBI", () => {
  it("muestra el título y la descripción de la sección", async () => {
    obtenerRol.mockReturnValue("usuario");
    obtenerTablerosPowerBi.mockResolvedValue(tableros);

    render(<PowerBI />);

    expect(
      await screen.findByRole("heading", { name: "Power BI" }),
    ).toBeInTheDocument();
    expect(
      screen.getByText("Consulta los informes y análisis interactivos de CASAGRES."),
    ).toBeInTheDocument();
  });

  it("selecciona por defecto el tablero más reciente y lo muestra en el iframe", async () => {
    obtenerRol.mockReturnValue("usuario");
    obtenerTablerosPowerBi.mockResolvedValue(tableros);

    render(<PowerBI />);

    const iframe = await screen.findByTitle("Ventas 2026");

    expect(iframe.tagName).toBe("IFRAME");
    expect(iframe).toHaveAttribute("src", "https://app.powerbi.com/view?r=nuevo");
  });

  it("permite cambiar de tablero desde el desplegable", async () => {
    obtenerRol.mockReturnValue("usuario");
    obtenerTablerosPowerBi.mockResolvedValue(tableros);
    const usuario = userEvent.setup();

    render(<PowerBI />);
    await screen.findByTitle("Ventas 2026");

    await usuario.selectOptions(screen.getByRole("combobox"), "1");

    const iframe = await screen.findByTitle("Ventas por los últimos años");
    expect(iframe).toHaveAttribute("src", "https://app.powerbi.com/view?r=viejo");
  });

  it("muestra un mensaje cuando no hay tableros", async () => {
    obtenerRol.mockReturnValue("usuario");
    obtenerTablerosPowerBi.mockResolvedValue([]);

    render(<PowerBI />);

    expect(
      await screen.findByText("No hay tableros disponibles"),
    ).toBeInTheDocument();
    expect(
      screen.getByText("Pídele a un administrador que agregue un tablero de Power BI."),
    ).toBeInTheDocument();
  });

  it("un usuario normal no ve los botones de agregar ni eliminar", async () => {
    obtenerRol.mockReturnValue("usuario");
    obtenerTablerosPowerBi.mockResolvedValue(tableros);

    render(<PowerBI />);
    await screen.findByTitle("Ventas 2026");

    expect(screen.queryByText("+ Agregar tablero")).not.toBeInTheDocument();
    expect(screen.queryByText("Eliminar tablero")).not.toBeInTheDocument();
  });

  it("un administrador puede agregar un tablero nuevo por URL", async () => {
    obtenerRol.mockReturnValue("admin");
    obtenerTablerosPowerBi.mockResolvedValue(tableros);
    agregarTableroPowerBi.mockResolvedValue({
      id: 3,
      nombre: "Tablero nuevo",
      url: "https://app.powerbi.com/view?r=recien-agregado",
    });
    const usuario = userEvent.setup();

    render(<PowerBI />);
    await screen.findByTitle("Ventas 2026");

    await usuario.click(screen.getByText("+ Agregar tablero"));

    await usuario.type(screen.getByPlaceholderText("Ej. Ventas por zona"), "Tablero nuevo");
    await usuario.type(
      screen.getByPlaceholderText("https://app.powerbi.com/view?r=..."),
      "https://app.powerbi.com/view?r=recien-agregado",
    );

    obtenerTablerosPowerBi.mockResolvedValue([
      {
        id: 3,
        nombre: "Tablero nuevo",
        url: "https://app.powerbi.com/view?r=recien-agregado",
        fechaCreacion: "2026-09-10T00:00:00Z",
      },
      ...tableros,
    ]);

    await usuario.click(screen.getByText("Guardar tablero"));

    expect(agregarTableroPowerBi).toHaveBeenCalledWith(
      "Tablero nuevo",
      "https://app.powerbi.com/view?r=recien-agregado",
    );

    expect(await screen.findByTitle("Tablero nuevo")).toBeInTheDocument();
  });

  it("muestra un error en el formulario si falta la URL", async () => {
    obtenerRol.mockReturnValue("admin");
    obtenerTablerosPowerBi.mockResolvedValue(tableros);
    const usuario = userEvent.setup();

    render(<PowerBI />);
    await screen.findByTitle("Ventas 2026");

    await usuario.click(screen.getByText("+ Agregar tablero"));
    await usuario.type(screen.getByPlaceholderText("Ej. Ventas por zona"), "Sin URL");
    await usuario.click(screen.getByText("Guardar tablero"));

    expect(
      await screen.findByText("El nombre y la URL son obligatorios."),
    ).toBeInTheDocument();
    expect(agregarTableroPowerBi).not.toHaveBeenCalled();
  });

  it("un administrador puede eliminar el tablero seleccionado, con confirmación", async () => {
    obtenerRol.mockReturnValue("admin");
    obtenerTablerosPowerBi.mockResolvedValue(tableros);
    eliminarTableroPowerBi.mockResolvedValue({ mensaje: "ok" });
    vi.spyOn(window, "confirm").mockReturnValue(true);
    const usuario = userEvent.setup();

    render(<PowerBI />);
    await screen.findByTitle("Ventas 2026");

    obtenerTablerosPowerBi.mockResolvedValue([tableros[1]]);

    await usuario.click(screen.getByText("Eliminar tablero"));

    expect(window.confirm).toHaveBeenCalledWith(
      '¿Eliminar el tablero "Ventas 2026"? Esta acción no se puede deshacer.',
    );
    expect(eliminarTableroPowerBi).toHaveBeenCalledWith(2);

    expect(await screen.findByTitle("Ventas por los últimos años")).toBeInTheDocument();
  });

  it("no elimina el tablero si el usuario cancela la confirmación", async () => {
    obtenerRol.mockReturnValue("admin");
    obtenerTablerosPowerBi.mockResolvedValue(tableros);
    vi.spyOn(window, "confirm").mockReturnValue(false);
    const usuario = userEvent.setup();

    render(<PowerBI />);
    await screen.findByTitle("Ventas 2026");

    await usuario.click(screen.getByText("Eliminar tablero"));

    expect(eliminarTableroPowerBi).not.toHaveBeenCalled();
  });

  it("muestra un mensaje de error si falla la carga de tableros", async () => {
    obtenerRol.mockReturnValue("usuario");
    obtenerTablerosPowerBi.mockRejectedValue(new Error("network error"));
    vi.spyOn(console, "error").mockImplementation(() => {});

    render(<PowerBI />);

    expect(
      await screen.findByText("No fue posible cargar los tableros de Power BI."),
    ).toBeInTheDocument();
  });
});
