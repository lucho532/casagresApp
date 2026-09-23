import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import Decisiones from "./Decisiones";
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
        { referencia: "REF1", pronostico: 500, metodo: "KRR", inferior: 400, superior: 600 },
        { referencia: "REF2", pronostico: 300, metodo: "ANIO_ANTERIOR" },
        { referencia: "REF3", pronostico: 200, metodo: "KRR" },
        { referencia: "REF4", pronostico: 50, metodo: null },
        { referencia: "REF5", pronostico: 0, metodo: "KRR" },
      ],
    },
  ],
};

afterEach(() => {
  vi.clearAllMocks();
});

function valorKpi(etiqueta) {
  const kpis = document.querySelector(".decisiones-kpis");
  const contenedor = within(kpis).getByText(etiqueta).closest(".decision-kpi");
  return contenedor.querySelector("strong").textContent;
}

async function esperarCarga() {
  return screen.findByText("Enfoque en decisiones");
}

describe("Decisiones", () => {
  it("muestra el estado de carga mientras llega el dashboard", () => {
    obtenerDashboard.mockReturnValue(new Promise(() => {}));

    render(<Decisiones mesSeleccionado="2025-01-01" />);

    expect(screen.getByText("Analizando información para decisiones...")).toBeInTheDocument();
  });

  it("muestra un mensaje de error cuando falla la carga", async () => {
    obtenerDashboard.mockRejectedValue(new Error("network error"));
    vi.spyOn(console, "error").mockImplementation(() => {});

    render(<Decisiones mesSeleccionado="2025-01-01" />);

    expect(
      await screen.findByText(
        "No fue posible cargar la información para la toma de decisiones.",
      ),
    ).toBeInTheDocument();
  });

  it("excluye productos con pronóstico cero y calcula los indicadores", async () => {
    obtenerDashboard.mockResolvedValue(dashboard);

    render(<Decisiones mesSeleccionado="2025-01-01" />);
    await esperarCarga();

    // Total: 500 + 300 + 200 + 50 = 1050 (REF5 excluida por pronóstico 0).
    expect(valorKpi("Demanda proyectada")).toBe("1.050");
    // Umbral de alta demanda = 1050 / 10 = 105 → REF1, REF2 y REF3 califican.
    expect(valorKpi("Alta demanda")).toBe("3");
    // Solo REF1 y REF3 usan KRR.
    expect(valorKpi("Modelo KRR")).toBe("2");
    // Solo REF1 tiene inferior y superior definidos.
    expect(valorKpi("Con intervalo")).toBe("1");
  });

  it("destaca la referencia con mayor demanda como foco principal", async () => {
    obtenerDashboard.mockResolvedValue(dashboard);

    render(<Decisiones mesSeleccionado="2025-01-01" />);
    await esperarCarga();

    const recomendacion = document.querySelector(".decision-recomendacion");
    expect(within(recomendacion).getByText("REF1")).toBeInTheDocument();
    expect(within(recomendacion).getByText("500")).toBeInTheDocument();
  });

  it("muestra la tabla completa ordenada de mayor a menor demanda, con la prioridad correcta", async () => {
    obtenerDashboard.mockResolvedValue(dashboard);

    render(<Decisiones mesSeleccionado="2025-01-01" />);
    await esperarCarga();

    const filas = screen.getAllByRole("row").slice(1); // sin el encabezado
    expect(filas).toHaveLength(4); // REF5 (pronóstico 0) queda excluida

    const referencias = filas.map((fila) => within(fila).getByRole("cell", { name: /REF\d/ }).textContent);
    expect(referencias).toEqual(["REF1", "REF2", "REF3", "REF4"]);

    // Las primeras tres posiciones son "Alta", la cuarta es "Media".
    expect(within(filas[0]).getByText("Alta")).toBeInTheDocument();
    expect(within(filas[2]).getByText("Alta")).toBeInTheDocument();
    expect(within(filas[3]).getByText("Media")).toBeInTheDocument();
  });

  it("muestra 'N/D' cuando el producto no tiene método asignado", async () => {
    obtenerDashboard.mockResolvedValue(dashboard);

    render(<Decisiones mesSeleccionado="2025-01-01" />);
    await esperarCarga();

    const filaRef4 = screen.getByText("REF4").closest("tr");
    expect(within(filaRef4).getByText("N/D")).toBeInTheDocument();
  });

  it("muestra un guion cuando el producto no tiene intervalo", async () => {
    obtenerDashboard.mockResolvedValue(dashboard);

    render(<Decisiones mesSeleccionado="2025-01-01" />);
    await esperarCarga();

    const filaRef2 = screen.getByText("REF2").closest("tr");
    expect(within(filaRef2).getByText("—")).toBeInTheDocument();
  });

  it("muestra un intervalo formateado cuando el producto sí lo tiene", async () => {
    obtenerDashboard.mockResolvedValue(dashboard);

    render(<Decisiones mesSeleccionado="2025-01-01" />);
    await esperarCarga();

    const tabla = screen.getByRole("table");
    const filaRef1 = within(tabla).getByText("REF1").closest("tr");
    expect(within(filaRef1).getByText("400 — 600")).toBeInTheDocument();
  });

  it("muestra el nombre del producto en vez de la referencia cuando el catálogo está disponible", async () => {
    obtenerDashboard.mockResolvedValue(dashboard);
    obtenerProductos.mockResolvedValue({
      productos: [{ referencia: "REF1", descripcion: "Teja de barro" }],
    });

    render(<Decisiones mesSeleccionado="2025-01-01" />);
    await esperarCarga();

    const recomendacion = document.querySelector(".decision-recomendacion");
    expect(within(recomendacion).getByText("Teja de barro")).toBeInTheDocument();

    const tabla = screen.getByRole("table");
    expect(within(tabla).getByText("Teja de barro")).toBeInTheDocument();
    expect(within(tabla).queryByText("REF1")).not.toBeInTheDocument();
  });

  it("muestra un estado vacío cuando el mes no tiene productos", async () => {
    obtenerDashboard.mockResolvedValue(dashboard);

    render(<Decisiones mesSeleccionado="2025-12-01" />);
    await esperarCarga();

    expect(screen.getByText("No hay información disponible")).toBeInTheDocument();
    expect(screen.getByText("0 productos")).toBeInTheDocument();
    expect(screen.queryAllByRole("row")).toHaveLength(1); // solo el encabezado
  });

  it("con mesesDisponibles, la tarjeta de periodo permite cambiar el mes seleccionado", async () => {
    obtenerDashboard.mockResolvedValue(dashboard);
    const setMesSeleccionado = vi.fn();
    const usuario = userEvent.setup();

    render(
      <Decisiones
        mesSeleccionado="2025-01-01"
        mesesDisponibles={[{ mes: "2025-01-01" }, { mes: "2025-02-01" }]}
        setMesSeleccionado={setMesSeleccionado}
      />,
    );
    await esperarCarga();

    const selectorPeriodo = screen.getByRole("combobox", { name: "Periodo analizado" });

    await usuario.selectOptions(selectorPeriodo, "2025-02-01");

    expect(setMesSeleccionado).toHaveBeenCalledWith("2025-02-01");
  });
});
