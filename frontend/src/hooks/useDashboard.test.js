import { renderHook, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { useDashboard } from "./useDashboard";
import { obtenerDashboard } from "../services/api";

vi.mock("../services/api", () => ({
  obtenerDashboard: vi.fn(),
}));

afterEach(() => {
  vi.resetAllMocks();
});

describe("useDashboard", () => {
  it("comienza en estado de carga, sin datos ni error", () => {
    obtenerDashboard.mockReturnValue(new Promise(() => {}));

    const { result } = renderHook(() => useDashboard());

    expect(result.current.cargando).toBe(true);
    expect(result.current.dashboard).toBeNull();
    expect(result.current.error).toBe("");
  });

  it("carga el dashboard exitosamente", async () => {
    const datos = { meses: [{ mes: "2025-01-01" }] };
    obtenerDashboard.mockResolvedValue(datos);

    const { result } = renderHook(() => useDashboard());

    await waitFor(() => expect(result.current.cargando).toBe(false));

    expect(result.current.dashboard).toEqual(datos);
    expect(result.current.error).toBe("");
  });

  it("usa el mensaje de error personalizado cuando falla la carga", async () => {
    obtenerDashboard.mockRejectedValue(new Error("network error"));
    vi.spyOn(console, "error").mockImplementation(() => {});

    const { result } = renderHook(() => useDashboard("Mensaje personalizado"));

    await waitFor(() => expect(result.current.cargando).toBe(false));

    expect(result.current.error).toBe("Mensaje personalizado");
    expect(result.current.dashboard).toBeNull();
  });

  it("usa el mensaje de error por defecto cuando no se especifica uno", async () => {
    obtenerDashboard.mockRejectedValue(new Error("network error"));
    vi.spyOn(console, "error").mockImplementation(() => {});

    const { result } = renderHook(() => useDashboard());

    await waitFor(() => expect(result.current.cargando).toBe(false));

    expect(result.current.error).toBe(
      "No fue posible cargar la información de ventas.",
    );
  });

  it("vuelve a pedir el dashboard cuando cambia el mensaje de error", async () => {
    obtenerDashboard.mockResolvedValue({ meses: [] });

    const { rerender } = renderHook(({ mensaje }) => useDashboard(mensaje), {
      initialProps: { mensaje: "Mensaje 1" },
    });

    await waitFor(() => expect(obtenerDashboard).toHaveBeenCalledTimes(1));

    rerender({ mensaje: "Mensaje 2" });

    await waitFor(() => expect(obtenerDashboard).toHaveBeenCalledTimes(2));
  });
});
