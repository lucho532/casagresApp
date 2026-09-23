import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import Inicio from "./Inicio";
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
        { referencia: "REF1", pronostico: 100, metodo: "KRR" },
        { referencia: "REF2", pronostico: 300, metodo: null },
        { referencia: "REF3", pronostico: 0, metodo: "KRR" },
      ],
    },
  ],
};

const propsBase = {
  cambiarPagina: vi.fn(),
  mesSeleccionado: "2025-01-01",
  setMesSeleccionado: vi.fn(),
  mesesDisponibles: [{ mes: "2025-01-01" }],
};

afterEach(() => {
  vi.clearAllMocks();
});

function valorKpi(etiqueta) {
  const contenedor = screen.getByText(etiqueta).closest(".inicio-kpi");
  return contenedor.querySelector("strong").textContent;
}

describe("Inicio", () => {
  it("muestra el estado de carga mientras llega el dashboard", () => {
    obtenerDashboard.mockReturnValue(new Promise(() => {}));

    render(<Inicio {...propsBase} />);

    expect(screen.getByText("Cargando resumen de la plataforma...")).toBeInTheDocument();
  });

  it("muestra un mensaje de error cuando falla la carga", async () => {
    obtenerDashboard.mockRejectedValue(new Error("network error"));
    vi.spyOn(console, "error").mockImplementation(() => {});

    render(<Inicio {...propsBase} />);

    expect(
      await screen.findByText("No fue posible cargar el resumen de la plataforma."),
    ).toBeInTheDocument();
  });

  it("excluye productos con pronóstico cero de los indicadores", async () => {
    obtenerDashboard.mockResolvedValue(dashboard);

    render(<Inicio {...propsBase} />);
    await screen.findByText("Resumen de la plataforma");

    // Solo REF1 y REF2 tienen pronóstico > 0.
    expect(valorKpi("Productos analizados")).toBe("2");
  });

  it("calcula la demanda total y el producto de mayor demanda", async () => {
    obtenerDashboard.mockResolvedValue(dashboard);

    render(<Inicio {...propsBase} />);

    await screen.findByText("Resumen de la plataforma");

    // 100 + 300 = 400
    expect(valorKpi("Demanda proyectada")).toBe("400");
    // REF2 es el de mayor demanda proyectada (300).
    expect(valorKpi("Mayor demanda")).toBe("300");

    const tarjetaMayorDemanda = screen.getByText("Mayor demanda").closest(".inicio-kpi");
    expect(tarjetaMayorDemanda.querySelector("small")).toHaveTextContent("REF2");
  });

  it("muestra el ranking de productos ordenado de mayor a menor demanda", async () => {
    obtenerDashboard.mockResolvedValue(dashboard);

    render(<Inicio {...propsBase} />);

    const lista = await screen.findByText("Productos con mayor demanda");
    const contenedor = lista.closest(".inicio-ranking").querySelector(".inicio-ranking-lista");
    const items = within(contenedor).getAllByText(/REF\d/);

    expect(items.map((el) => el.textContent)).toEqual(["REF2", "REF1"]);
  });

  it("muestra 'Sin método' cuando el producto no tiene método asignado", async () => {
    obtenerDashboard.mockResolvedValue(dashboard);

    render(<Inicio {...propsBase} />);

    expect(await screen.findByText("Sin método")).toBeInTheDocument();
  });

  it("muestra el estado vacío cuando el mes seleccionado no tiene productos", async () => {
    obtenerDashboard.mockResolvedValue(dashboard);

    render(<Inicio {...propsBase} mesSeleccionado="2025-12-01" />);

    expect(await screen.findByText("No hay productos disponibles.")).toBeInTheDocument();
    expect(screen.getByText("Sin datos")).toBeInTheDocument();
  });

  it("al elegir otro periodo en el selector, llama a setMesSeleccionado", async () => {
    const setMesSeleccionado = vi.fn();
    obtenerDashboard.mockResolvedValue({
      meses: [...dashboard.meses, { mes: "2025-02-01", productos: [] }],
    });
    const usuario = userEvent.setup();

    render(
      <Inicio
        {...propsBase}
        setMesSeleccionado={setMesSeleccionado}
        mesesDisponibles={[{ mes: "2025-01-01" }, { mes: "2025-02-01" }]}
      />,
    );

    await screen.findByText("Resumen de la plataforma");

    await usuario.selectOptions(screen.getByRole("combobox"), "2025-02-01");

    expect(setMesSeleccionado).toHaveBeenCalledWith("2025-02-01");
  });

  it("muestra el nombre del producto en vez de la referencia cuando el catálogo está disponible", async () => {
    obtenerDashboard.mockResolvedValue(dashboard);
    obtenerProductos.mockResolvedValue({
      productos: [
        { referencia: "REF1", descripcion: "Teja de barro" },
        { referencia: "REF2", descripcion: "Ladrillo hueco" },
      ],
    });

    render(<Inicio {...propsBase} />);

    const tarjetaMayorDemanda = (
      await screen.findByText("Mayor demanda")
    ).closest(".inicio-kpi");

    expect(tarjetaMayorDemanda).toHaveTextContent("Ladrillo hueco");

    const lista = screen
      .getByText("Productos con mayor demanda")
      .closest(".inicio-ranking")
      .querySelector(".inicio-ranking-lista");

    expect(within(lista).getByText("Ladrillo hueco")).toBeInTheDocument();
    expect(within(lista).getByText("Teja de barro")).toBeInTheDocument();
    expect(screen.queryByText("REF1")).not.toBeInTheDocument();
    expect(screen.queryByText("REF2")).not.toBeInTheDocument();
  });

  it("al hacer clic en un acceso rápido, llama a cambiarPagina con el id correspondiente", async () => {
    const cambiarPagina = vi.fn();
    obtenerDashboard.mockResolvedValue(dashboard);
    const usuario = userEvent.setup();

    render(<Inicio {...propsBase} cambiarPagina={cambiarPagina} />);
    await screen.findByText("Resumen de la plataforma");

    await usuario.click(screen.getByText("Demanda futura"));

    expect(cambiarPagina).toHaveBeenCalledWith("demanda");
  });
});
