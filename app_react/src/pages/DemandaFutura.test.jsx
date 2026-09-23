import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import DemandaFutura from "./DemandaFutura";
import { obtenerDashboard, obtenerProductos } from "../services/api";

vi.mock("../services/api", () => ({
  obtenerDashboard: vi.fn(),
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
        {
          referencia: "REF1",
          pronostico: 500,
          inferior: 400,
          superior: 600,
          metodo: "KRR",
          alphaAci: 0.1,
        },
        {
          referencia: "REF2",
          pronostico: 300,
          inferior: 250,
          superior: 350,
          metodo: "ANIO_ANTERIOR",
          alphaAci: null,
        },
        { referencia: "REF3", pronostico: 0, metodo: "KRR" },
      ],
    },
  ],
};

afterEach(() => {
  vi.clearAllMocks();
});

function valorCard(etiqueta) {
  const contenedor = screen.getByText(etiqueta).closest(".demanda-card");
  return contenedor.querySelector("strong").textContent;
}

async function esperarCarga() {
  return screen.findByText("Demanda futura");
}

describe("DemandaFutura", () => {
  it("muestra el estado de carga mientras llega el dashboard", () => {
    obtenerDashboard.mockReturnValue(new Promise(() => {}));

    render(<DemandaFutura mesSeleccionado="2025-01-01" />);

    expect(screen.getByText("Cargando estimaciones de demanda...")).toBeInTheDocument();
  });

  it("muestra un mensaje de error cuando falla la carga", async () => {
    obtenerDashboard.mockRejectedValue(new Error("network error"));
    vi.spyOn(console, "error").mockImplementation(() => {});

    render(<DemandaFutura mesSeleccionado="2025-01-01" />);

    expect(
      await screen.findByText("No fue posible cargar las estimaciones de demanda."),
    ).toBeInTheDocument();
  });

  it("al cargar, selecciona automáticamente el producto de mayor demanda", async () => {
    obtenerDashboard.mockResolvedValue(dashboard);

    render(<DemandaFutura mesSeleccionado="2025-01-01" />);
    await esperarCarga();

    const principal = document.querySelector(".tarjeta-demanda-principal");
    expect(within(principal).getByText("REF1")).toBeInTheDocument();
  });

  it("excluye del selector los productos con pronóstico cero", async () => {
    obtenerDashboard.mockResolvedValue(dashboard);

    render(<DemandaFutura mesSeleccionado="2025-01-01" />);
    await esperarCarga();

    const opciones = screen.getAllByRole("option").map((op) => op.textContent);
    expect(opciones).toEqual(["REF1", "REF2"]);
  });

  it("muestra la demanda estimada y el rango del producto seleccionado", async () => {
    obtenerDashboard.mockResolvedValue(dashboard);

    render(<DemandaFutura mesSeleccionado="2025-01-01" />);
    await esperarCarga();

    expect(valorCard("Demanda estimada")).toBe("500");
    expect(valorCard("Escenario mínimo")).toBe("400");
    expect(valorCard("Escenario máximo")).toBe("600");
    expect(valorCard("Confianza")).toBe("90%");
  });

  it("muestra '-' en confianza cuando el producto no tiene alphaAci", async () => {
    obtenerDashboard.mockResolvedValue(dashboard);
    const usuario = userEvent.setup();

    render(<DemandaFutura mesSeleccionado="2025-01-01" />);
    await esperarCarga();

    await usuario.selectOptions(screen.getByRole("combobox"), "REF2");

    expect(valorCard("Confianza")).toBe("-");
  });

  it("muestra el badge de método correspondiente al producto seleccionado", async () => {
    obtenerDashboard.mockResolvedValue(dashboard);

    render(<DemandaFutura mesSeleccionado="2025-01-01" />);
    await esperarCarga();

    const principal = document.querySelector(".tarjeta-demanda-principal");
    expect(within(principal).getByText("KRR")).toHaveClass("metodo-krr");
  });

  it("muestra 'Sin método' cuando el producto no tiene método asignado", async () => {
    obtenerDashboard.mockResolvedValue({
      meses: [
        {
          mes: "2025-01-01",
          productos: [{ referencia: "REF1", pronostico: 500, metodo: null }],
        },
      ],
    });

    render(<DemandaFutura mesSeleccionado="2025-01-01" />);
    await esperarCarga();

    const principal = document.querySelector(".tarjeta-demanda-principal");
    expect(within(principal).getByText("Sin método")).toBeInTheDocument();
  });

  it("al cambiar el producto en el selector, actualiza la información mostrada", async () => {
    obtenerDashboard.mockResolvedValue(dashboard);
    const usuario = userEvent.setup();

    render(<DemandaFutura mesSeleccionado="2025-01-01" />);
    await esperarCarga();

    await usuario.selectOptions(screen.getByRole("combobox"), "REF2");

    const principal = document.querySelector(".tarjeta-demanda-principal");
    expect(within(principal).getByText("REF2")).toBeInTheDocument();
    expect(valorCard("Demanda estimada")).toBe("300");
  });

  it("muestra la información del modelo para el producto seleccionado", async () => {
    obtenerDashboard.mockResolvedValue(dashboard);

    render(<DemandaFutura mesSeleccionado="2025-01-01" />);
    await esperarCarga();

    const modeloInfo = document.querySelector(".modelo-info");
    expect(within(modeloInfo).getByText("KRR")).toBeInTheDocument();
    expect(within(modeloInfo).getByText("90%")).toBeInTheDocument();
    expect(within(modeloInfo).getByText("Procesado")).toBeInTheDocument();
  });

  it("muestra el nombre del producto en vez de la referencia cuando el catálogo está disponible", async () => {
    obtenerDashboard.mockResolvedValue(dashboard);
    obtenerProductos.mockResolvedValue({
      productos: [{ referencia: "REF1", descripcion: "Teja de barro" }],
    });

    render(<DemandaFutura mesSeleccionado="2025-01-01" />);
    await esperarCarga();

    const principal = document.querySelector(".tarjeta-demanda-principal");
    expect(within(principal).getByText("Teja de barro")).toBeInTheDocument();
    expect(screen.getByRole("option", { name: "Teja de barro" })).toBeInTheDocument();
  });

  it("muestra un estado vacío cuando no hay pronósticos disponibles", async () => {
    obtenerDashboard.mockResolvedValue({
      meses: [{ mes: "2025-01-01", productos: [] }],
    });

    render(<DemandaFutura mesSeleccionado="2025-01-01" />);
    await esperarCarga();

    expect(
      screen.getByText("No hay pronósticos disponibles"),
    ).toBeInTheDocument();
  });

  it("con mesesDisponibles, la tarjeta de periodo permite cambiar el mes seleccionado", async () => {
    obtenerDashboard.mockResolvedValue(dashboard);
    const setMesSeleccionado = vi.fn();
    const usuario = userEvent.setup();

    render(
      <DemandaFutura
        mesSeleccionado="2025-01-01"
        mesesDisponibles={[{ mes: "2025-01-01" }, { mes: "2025-02-01" }]}
        setMesSeleccionado={setMesSeleccionado}
      />,
    );
    await esperarCarga();

    const selectorPeriodo = screen.getByRole("combobox", { name: "Periodo proyectado" });

    await usuario.selectOptions(selectorPeriodo, "2025-02-01");

    expect(setMesSeleccionado).toHaveBeenCalledWith("2025-02-01");
  });
});
