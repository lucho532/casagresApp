import { describe, expect, it } from "vitest";
import { formatearCantidad, formatearMes, formatearNumero } from "./formato";

describe("formatearNumero", () => {
  it("muestra un guion cuando el valor es null", () => {
    expect(formatearNumero(null)).toBe("-");
  });

  it("muestra un guion cuando el valor es undefined", () => {
    expect(formatearNumero(undefined)).toBe("-");
  });

  it("redondea y formatea con separador de miles", () => {
    expect(formatearNumero(1234.56)).toBe("1.235");
  });

  it("formatea el cero como 0, no como guion", () => {
    expect(formatearNumero(0)).toBe("0");
  });

  it("formatea números negativos", () => {
    expect(formatearNumero(-15.4)).toBe("-15");
  });
});

describe("formatearCantidad", () => {
  it("trata null como cero en vez de mostrar un guion", () => {
    expect(formatearCantidad(null)).toBe("0");
  });

  it("trata undefined como cero", () => {
    expect(formatearCantidad(undefined)).toBe("0");
  });

  it("redondea y formatea con separador de miles", () => {
    expect(formatearCantidad(2500.9)).toBe("2.501");
  });
});

describe("formatearMes", () => {
  it("muestra un guion cuando no hay mes", () => {
    expect(formatearMes(null)).toBe("-");
    expect(formatearMes(undefined)).toBe("-");
    expect(formatearMes("")).toBe("-");
  });

  it("formatea una fecha ISO como mes y año en español", () => {
    expect(formatearMes("2025-03-01")).toBe("marzo de 2025");
  });

  it("formatea correctamente el primer mes del año", () => {
    expect(formatearMes("2025-01-01")).toBe("enero de 2025");
  });
});
