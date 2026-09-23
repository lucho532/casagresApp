import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import Productos from "./Productos";
import { obtenerProductos } from "../services/api";

vi.mock("../services/api", () => ({
  obtenerProductos: vi.fn(),
}));

const productos = [
  {
    referencia: "REF1",
    descripcion: "Tubo PVC",
    marca: "Marca A",
    linea: "Linea A",
    grupo: "Grupo A",
    clase: "Clase A",
    planta: "Planta A",
  },
  {
    referencia: "REF2",
    descripcion: "Codo PVC",
    marca: "Marca B",
    linea: "Linea B",
    grupo: "Grupo B",
    clase: "Clase B",
    planta: "Planta B",
  },
];

function generarProductos(cantidad) {
  return Array.from({ length: cantidad }, (_, indice) => {
    const numero = String(indice + 1).padStart(3, "0");

    return {
      referencia: `REF${numero}`,
      descripcion: `Producto ${numero}`,
      marca: "Marca",
      linea: "Linea",
      grupo: "Grupo",
      planta: "Planta",
    };
  });
}

async function esperarCarga() {
  return screen.findByText("Productos disponibles");
}

afterEach(() => {
  vi.clearAllMocks();
});

describe("Productos", () => {
  it("muestra el estado de carga mientras llega la respuesta", () => {
    obtenerProductos.mockReturnValue(new Promise(() => {}));

    render(<Productos />);

    expect(screen.getByText("Cargando catálogo de productos...")).toBeInTheDocument();
  });

  it("si la carga tarda más de 1.2s, avisa que puede estar importando desde Excel", async () => {
    obtenerProductos.mockReturnValue(new Promise(() => {}));

    render(<Productos />);

    expect(
      await screen.findByText(
        /Importando el catálogo desde Excel por primera vez/,
        {},
        { timeout: 2000 },
      ),
    ).toBeInTheDocument();
  });

  it("muestra un mensaje de error cuando falla la carga", async () => {
    obtenerProductos.mockRejectedValue(new Error("network error"));
    vi.spyOn(console, "error").mockImplementation(() => {});

    render(<Productos />);

    expect(
      await screen.findByText("No fue posible obtener el catálogo de productos."),
    ).toBeInTheDocument();
  });

  it("al cargar, selecciona automáticamente el primer producto", async () => {
    obtenerProductos.mockResolvedValue({ productos });

    render(<Productos />);
    await esperarCarga();

    const detalle = screen.getByText("Marca A").closest(".producto-detalle");
    expect(within(detalle).getByText("Tubo PVC")).toBeInTheDocument();
  });

  it("muestra el estado vacío cuando el catálogo no tiene productos", async () => {
    obtenerProductos.mockResolvedValue({ productos: [] });

    render(<Productos />);

    expect(await screen.findByText("Selecciona un producto")).toBeInTheDocument();
    expect(screen.getByText("No se encontraron productos.")).toBeInTheDocument();
  });

  it("filtra la tabla por referencia o descripción", async () => {
    obtenerProductos.mockResolvedValue({ productos });
    const usuario = userEvent.setup();

    render(<Productos />);
    await esperarCarga();

    await usuario.type(
      screen.getByPlaceholderText("Buscar por referencia o nombre del producto..."),
      "codo",
    );

    const tabla = screen.getByRole("table");
    expect(within(tabla).queryByText("Tubo PVC")).not.toBeInTheDocument();
    expect(within(tabla).getByText("Codo PVC")).toBeInTheDocument();
    expect(document.querySelector(".productos-resultados")).toHaveTextContent(
      "Mostrando 1 de 2 productos",
    );
  });

  it("muestra 'No se encontraron productos' cuando el filtro no tiene coincidencias", async () => {
    obtenerProductos.mockResolvedValue({ productos });
    const usuario = userEvent.setup();

    render(<Productos />);
    await esperarCarga();

    await usuario.type(
      screen.getByPlaceholderText("Buscar por referencia o nombre del producto..."),
      "no-existe",
    );

    expect(screen.getByText("No se encontraron productos.")).toBeInTheDocument();
  });

  it("el botón de limpiar búsqueda aparece solo con texto y la vacía al hacer clic", async () => {
    obtenerProductos.mockResolvedValue({ productos });
    const usuario = userEvent.setup();

    render(<Productos />);
    await esperarCarga();

    const input = screen.getByPlaceholderText(
      "Buscar por referencia o nombre del producto...",
    );

    expect(screen.queryByTitle("Limpiar búsqueda")).not.toBeInTheDocument();

    await usuario.type(input, "codo");
    await usuario.click(screen.getByTitle("Limpiar búsqueda"));

    expect(input).toHaveValue("");
    expect(within(screen.getByRole("table")).getByText("Tubo PVC")).toBeInTheDocument();
  });

  it("al hacer clic en una fila, muestra el detalle de ese producto", async () => {
    obtenerProductos.mockResolvedValue({ productos });
    const usuario = userEvent.setup();

    render(<Productos />);
    await esperarCarga();

    await usuario.click(screen.getByText("Codo PVC"));

    const detalle = screen.getByText("Marca B").closest(".producto-detalle");
    expect(within(detalle).getByText("Codo PVC")).toBeInTheDocument();
  });

  describe("paginación", () => {
    it("con pocos productos, muestra una sola página y deshabilita ambos botones", async () => {
      obtenerProductos.mockResolvedValue({ productos });

      render(<Productos />);
      await esperarCarga();

      expect(screen.getByText("Página 1 de 1")).toBeInTheDocument();
      expect(screen.getByText("‹ Anterior")).toBeDisabled();
      expect(screen.getByText("Siguiente ›")).toBeDisabled();
    });

    it("no muestra los controles de paginación cuando no hay resultados", async () => {
      obtenerProductos.mockResolvedValue({ productos: [] });

      render(<Productos />);
      await esperarCarga();

      expect(screen.queryByText(/Página \d+ de \d+/)).not.toBeInTheDocument();
    });

    it("muestra solo el tamaño de página elegido (20 por defecto) y el indicador correcto", async () => {
      obtenerProductos.mockResolvedValue({ productos: generarProductos(45) });

      render(<Productos />);
      await esperarCarga();

      const tabla = screen.getByRole("table");
      expect(within(tabla).getAllByRole("row")).toHaveLength(21); // encabezado + 20 filas
      expect(screen.getByText("Página 1 de 3")).toBeInTheDocument();
      expect(within(tabla).getByText("Producto 001")).toBeInTheDocument();
      expect(within(tabla).queryByText("Producto 021")).not.toBeInTheDocument();
    });

    it("al hacer clic en 'Siguiente', muestra la página siguiente y habilita 'Anterior'", async () => {
      obtenerProductos.mockResolvedValue({ productos: generarProductos(45) });
      const usuario = userEvent.setup();

      render(<Productos />);
      await esperarCarga();

      const botonAnterior = screen.getByText("‹ Anterior");
      expect(botonAnterior).toBeDisabled();

      await usuario.click(screen.getByText("Siguiente ›"));

      expect(screen.getByText("Página 2 de 3")).toBeInTheDocument();
      expect(botonAnterior).not.toBeDisabled();

      const tabla = screen.getByRole("table");
      expect(within(tabla).getByText("Producto 021")).toBeInTheDocument();
      expect(within(tabla).queryByText("Producto 001")).not.toBeInTheDocument();
    });

    it("deshabilita 'Siguiente' en la última página", async () => {
      obtenerProductos.mockResolvedValue({ productos: generarProductos(45) });
      const usuario = userEvent.setup();

      render(<Productos />);
      await esperarCarga();

      await usuario.click(screen.getByText("Siguiente ›"));
      await usuario.click(screen.getByText("Siguiente ›"));

      expect(screen.getByText("Página 3 de 3")).toBeInTheDocument();
      expect(screen.getByText("Siguiente ›")).toBeDisabled();

      const tabla = screen.getByRole("table");
      expect(within(tabla).getByText("Producto 045")).toBeInTheDocument();
    });

    it("al cambiar el tamaño de página, actualiza la cantidad de filas y vuelve a la página 1", async () => {
      obtenerProductos.mockResolvedValue({ productos: generarProductos(45) });
      const usuario = userEvent.setup();

      render(<Productos />);
      await esperarCarga();

      await usuario.click(screen.getByText("Siguiente ›"));
      expect(screen.getByText("Página 2 de 3")).toBeInTheDocument();

      await usuario.selectOptions(screen.getByLabelText("Mostrar"), "50");

      expect(screen.getByText("Página 1 de 1")).toBeInTheDocument();

      const tabla = screen.getByRole("table");
      expect(within(tabla).getAllByRole("row")).toHaveLength(46); // encabezado + 45 filas
    });

    it("al buscar, vuelve a la página 1", async () => {
      obtenerProductos.mockResolvedValue({ productos: generarProductos(45) });
      const usuario = userEvent.setup();

      render(<Productos />);
      await esperarCarga();

      await usuario.click(screen.getByText("Siguiente ›"));
      expect(screen.getByText("Página 2 de 3")).toBeInTheDocument();

      await usuario.type(
        screen.getByPlaceholderText("Buscar por referencia o nombre del producto..."),
        "Producto 044",
      );

      expect(screen.getByText("Página 1 de 1")).toBeInTheDocument();
    });
  });
});
