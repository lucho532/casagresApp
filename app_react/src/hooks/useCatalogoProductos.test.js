import { renderHook, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { useCatalogoProductos } from "./useCatalogoProductos";
import { obtenerProductos } from "../services/api";

vi.mock("../services/api", () => ({
  obtenerProductos: vi.fn(),
}));

afterEach(() => {
  vi.resetAllMocks();
});

describe("useCatalogoProductos", () => {
  it("antes de cargar el catálogo, devuelve la referencia tal cual", () => {
    obtenerProductos.mockReturnValue(new Promise(() => {}));

    const { result } = renderHook(() => useCatalogoProductos());

    expect(result.current.obtenerNombreProducto("REF1")).toBe("REF1");
  });

  it("una vez cargado el catálogo, devuelve el nombre del producto", async () => {
    obtenerProductos.mockResolvedValue({
      productos: [
        { referencia: "REF1", descripcion: "Teja de barro" },
        { referencia: "REF2", descripcion: "Ladrillo hueco" },
      ],
    });

    const { result } = renderHook(() => useCatalogoProductos());

    await waitFor(() =>
      expect(result.current.obtenerNombreProducto("REF1")).toBe(
        "Teja de barro",
      ),
    );

    expect(result.current.obtenerNombreProducto("REF2")).toBe(
      "Ladrillo hueco",
    );
  });

  it("si una referencia no está en el catálogo, devuelve la referencia tal cual", async () => {
    obtenerProductos.mockResolvedValue({
      productos: [{ referencia: "REF1", descripcion: "Teja de barro" }],
    });

    const { result } = renderHook(() => useCatalogoProductos());

    await waitFor(() => expect(obtenerProductos).toHaveBeenCalled());

    expect(result.current.obtenerNombreProducto("REF-DESCONOCIDA")).toBe(
      "REF-DESCONOCIDA",
    );
  });

  it("si falla la carga del catálogo, sigue devolviendo la referencia tal cual", async () => {
    obtenerProductos.mockRejectedValue(new Error("network error"));
    vi.spyOn(console, "error").mockImplementation(() => {});

    const { result } = renderHook(() => useCatalogoProductos());

    await waitFor(() => expect(obtenerProductos).toHaveBeenCalled());

    expect(result.current.obtenerNombreProducto("REF1")).toBe("REF1");
  });

  describe("obtenerNombreConCodigo", () => {
    it("antes de cargar el catálogo, devuelve solo la referencia (sin duplicarla)", () => {
      obtenerProductos.mockReturnValue(new Promise(() => {}));

      const { result } = renderHook(() => useCatalogoProductos());

      expect(result.current.obtenerNombreConCodigo("REF1")).toBe("REF1");
    });

    it("una vez cargado el catálogo, combina el nombre y el código", async () => {
      obtenerProductos.mockResolvedValue({
        productos: [{ referencia: "REF1", descripcion: "Teja de barro" }],
      });

      const { result } = renderHook(() => useCatalogoProductos());

      await waitFor(() =>
        expect(result.current.obtenerNombreConCodigo("REF1")).toBe(
          "Teja de barro · REF1",
        ),
      );
    });
  });
});
