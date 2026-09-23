import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import Tendencias from "./Tendencias";
import { obtenerDashboard, obtenerHistorico, obtenerProductos } from "../services/api";

vi.mock("../services/api", () => ({
  obtenerDashboard: vi.fn(),
  obtenerHistorico: vi.fn(),
  obtenerProductos: vi.fn(),
}));

// Por defecto, sin catálogo: los nombres de producto caen de vuelta a la
// referencia, que es justo lo que ya esperan las pruebas existentes.
obtenerProductos.mockResolvedValue({ productos: [] });

const dashboard = {
  meses: [
    {
      mes: "2025-01-01",
      productos: [
        { referencia: "REF1", pronostico: 500 },
        { referencia: "REF2", pronostico: 300 },
      ],
    },
  ],
};

const historicoRef1 = {
  historico: [
    { mes: "2024-11-01", cantidad: 100 },
    { mes: "2024-12-01", cantidad: 200 },
  ],
};

afterEach(() => {
  vi.clearAllMocks();
});

function valorInfo(etiqueta) {
  const contenedor = screen.getByText(etiqueta).closest(".info-producto");
  return contenedor.querySelector("strong").textContent;
}

async function esperarCarga() {
  return screen.findByText("Tendencias de compra");
}

describe("Tendencias", () => {
  it("muestra el estado de carga mientras llega el dashboard", () => {
    obtenerDashboard.mockReturnValue(new Promise(() => {}));

    render(<Tendencias mesSeleccionado="2025-01-01" />);

    expect(screen.getByText("Cargando tendencias de ventas...")).toBeInTheDocument();
    expect(obtenerHistorico).not.toHaveBeenCalled();
  });

  it("muestra un mensaje de error cuando falla la carga del dashboard", async () => {
    obtenerDashboard.mockRejectedValue(new Error("network error"));
    vi.spyOn(console, "error").mockImplementation(() => {});

    render(<Tendencias mesSeleccionado="2025-01-01" />);

    expect(
      await screen.findByText("No fue posible obtener la información de ventas."),
    ).toBeInTheDocument();
  });

  it("al cargar, selecciona el producto de mayor demanda y obtiene su histórico", async () => {
    obtenerDashboard.mockResolvedValue(dashboard);
    obtenerHistorico.mockResolvedValue(historicoRef1);

    render(<Tendencias mesSeleccionado="2025-01-01" />);
    await esperarCarga();

    expect(obtenerHistorico).toHaveBeenCalledWith("REF1");
    expect(await screen.findByText("100")).toBeInTheDocument();
    expect(valorInfo("Producto seleccionado")).toBe("REF1");
  });

  it("selecciona por defecto el producto de mayor demanda aunque no sea el primero de la lista", async () => {
    obtenerDashboard.mockResolvedValue({
      meses: [
        {
          mes: "2025-01-01",
          productos: [
            { referencia: "REF1", pronostico: 100 },
            { referencia: "REF2", pronostico: 900 },
            { referencia: "REF3", pronostico: 400 },
          ],
        },
      ],
    });
    obtenerHistorico.mockResolvedValue(historicoRef1);

    render(<Tendencias mesSeleccionado="2025-01-01" />);
    await esperarCarga();

    expect(obtenerHistorico).toHaveBeenCalledWith("REF2");
    await screen.findByText("100");
    expect(valorInfo("Producto seleccionado")).toBe("REF2");
  });

  it("muestra 'Cargando histórico...' mientras se obtiene el histórico del producto", async () => {
    obtenerDashboard.mockResolvedValue(dashboard);
    obtenerHistorico.mockReturnValue(new Promise(() => {}));

    render(<Tendencias mesSeleccionado="2025-01-01" />);
    await esperarCarga();

    expect(await screen.findByText("Cargando histórico...")).toBeInTheDocument();
  });

  it("muestra un mensaje de error cuando falla la carga del histórico", async () => {
    obtenerDashboard.mockResolvedValue(dashboard);
    obtenerHistorico.mockRejectedValue(new Error("network error"));
    vi.spyOn(console, "error").mockImplementation(() => {});

    render(<Tendencias mesSeleccionado="2025-01-01" />);
    await esperarCarga();

    expect(
      await screen.findByText("No fue posible obtener el histórico del producto."),
    ).toBeInTheDocument();
  });

  it("calcula el total vendido y el promedio mensual", async () => {
    obtenerDashboard.mockResolvedValue(dashboard);
    obtenerHistorico.mockResolvedValue(historicoRef1);

    render(<Tendencias mesSeleccionado="2025-01-01" />);
    await esperarCarga();

    await screen.findByText("100");

    expect(valorInfo("Meses analizados")).toBe("2");
    expect(valorInfo("Total vendido")).toBe("300");
    expect(valorInfo("Promedio mensual")).toBe("150");
  });

  it("muestra la tabla histórica con mes y cantidad", async () => {
    obtenerDashboard.mockResolvedValue(dashboard);
    obtenerHistorico.mockResolvedValue(historicoRef1);

    render(<Tendencias mesSeleccionado="2025-01-01" />);
    await esperarCarga();
    await screen.findByText("2024-11-01");

    const tabla = screen.getByRole("table");
    const filas = within(tabla).getAllByRole("row").slice(1);

    expect(filas).toHaveLength(2);
    expect(within(filas[0]).getByText("2024-11-01")).toBeInTheDocument();
    expect(within(filas[0]).getByText("100")).toBeInTheDocument();
    expect(within(filas[1]).getByText("2024-12-01")).toBeInTheDocument();
    expect(within(filas[1]).getByText("200")).toBeInTheDocument();
  });

  it("al cambiar el producto en el selector, obtiene el histórico del nuevo producto", async () => {
    obtenerDashboard.mockResolvedValue(dashboard);
    obtenerHistorico.mockResolvedValue(historicoRef1);
    const usuario = userEvent.setup();

    render(<Tendencias mesSeleccionado="2025-01-01" />);
    await esperarCarga();
    await screen.findByText("100");

    obtenerHistorico.mockResolvedValue({
      historico: [{ mes: "2024-12-01", cantidad: 50 }],
    });

    await usuario.selectOptions(screen.getByRole("combobox"), "REF2");

    expect(obtenerHistorico).toHaveBeenCalledWith("REF2");
    await screen.findByRole("row", { name: /2024-12-01/ });
    expect(valorInfo("Producto seleccionado")).toBe("REF2");
    expect(valorInfo("Total vendido")).toBe("50");
  });

  it("muestra el nombre del producto en vez de la referencia cuando el catálogo está disponible", async () => {
    obtenerDashboard.mockResolvedValue(dashboard);
    obtenerHistorico.mockResolvedValue(historicoRef1);
    obtenerProductos.mockResolvedValue({
      productos: [{ referencia: "REF1", descripcion: "Teja de barro" }],
    });

    render(<Tendencias mesSeleccionado="2025-01-01" />);
    await esperarCarga();
    await screen.findByText("100");

    expect(valorInfo("Producto seleccionado")).toBe("Teja de barro");
    expect(screen.getByRole("option", { name: "Teja de barro" })).toBeInTheDocument();

    // El código del producto se sigue mostrando junto al nombre.
    const contenedor = screen
      .getByText("Producto seleccionado")
      .closest(".info-producto");
    expect(contenedor).toHaveTextContent("REF1");
  });

  it("no muestra el panel de información del producto cuando no hay productos", async () => {
    obtenerDashboard.mockResolvedValue({
      meses: [{ mes: "2025-01-01", productos: [] }],
    });

    render(<Tendencias mesSeleccionado="2025-01-01" />);
    await esperarCarga();

    expect(screen.queryByText("Producto seleccionado")).not.toBeInTheDocument();
    expect(obtenerHistorico).not.toHaveBeenCalled();
  });

  it("con mesesDisponibles, la tarjeta de periodo permite cambiar el mes seleccionado", async () => {
    obtenerDashboard.mockResolvedValue(dashboard);
    obtenerHistorico.mockResolvedValue(historicoRef1);
    const setMesSeleccionado = vi.fn();
    const usuario = userEvent.setup();

    render(
      <Tendencias
        mesSeleccionado="2025-01-01"
        mesesDisponibles={[{ mes: "2025-01-01" }, { mes: "2025-02-01" }]}
        setMesSeleccionado={setMesSeleccionado}
      />,
    );
    await esperarCarga();

    const selectorPeriodo = screen.getByRole("combobox", { name: "Horizonte mostrado" });

    await usuario.selectOptions(selectorPeriodo, "2025-02-01");

    expect(setMesSeleccionado).toHaveBeenCalledWith("2025-02-01");
  });
});
