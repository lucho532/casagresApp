import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { obtenerNombre, obtenerRol, obtenerToken } from "./auth";

const ROL_CLAIM = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

function crearTokenFalso(payload) {
  // Un JWT real codifica el payload en UTF-8 antes de aplicar base64url;
  // hay que replicar eso para que obtenerNombre() (que decodifica UTF-8)
  // pueda leer correctamente caracteres acentuados.
  const bytesUtf8 = new TextEncoder().encode(JSON.stringify(payload));

  let binario = "";
  bytesUtf8.forEach((byte) => {
    binario += String.fromCharCode(byte);
  });

  const base64 = btoa(binario)
    .replace(/\+/g, "-")
    .replace(/\//g, "_")
    .replace(/=+$/, "");

  return `header.${base64}.firma`;
}

beforeEach(() => {
  sessionStorage.clear();
});

describe("obtenerToken", () => {
  it("devuelve null cuando no hay token guardado", () => {
    expect(obtenerToken()).toBeNull();
  });

  it("devuelve el token guardado en sessionStorage", () => {
    sessionStorage.setItem("token", "un-token-cualquiera");

    expect(obtenerToken()).toBe("un-token-cualquiera");
  });
});

describe("obtenerRol", () => {
  it("devuelve null cuando no hay token", () => {
    expect(obtenerRol()).toBeNull();
  });

  it("devuelve el rol contenido en el token", () => {
    sessionStorage.setItem("token", crearTokenFalso({ [ROL_CLAIM]: "admin" }));

    expect(obtenerRol()).toBe("admin");
  });

  it("devuelve null cuando el token no contiene el claim de rol", () => {
    sessionStorage.setItem("token", crearTokenFalso({ nombre: "Juan" }));

    expect(obtenerRol()).toBeNull();
  });

  it("devuelve null y no lanza excepción cuando el token está corrupto", () => {
    vi.spyOn(console, "error").mockImplementation(() => {});
    sessionStorage.setItem("token", "esto-no-es-un-jwt-valido");

    expect(obtenerRol()).toBeNull();

    vi.restoreAllMocks();
  });
});

describe("obtenerNombre", () => {
  it("devuelve null cuando no hay token", () => {
    expect(obtenerNombre()).toBeNull();
  });

  it("devuelve el nombre contenido en el token", () => {
    sessionStorage.setItem("token", crearTokenFalso({ nombre: "Juan Pérez" }));

    expect(obtenerNombre()).toBe("Juan Pérez");
  });

  it("decodifica correctamente caracteres acentuados", () => {
    sessionStorage.setItem("token", crearTokenFalso({ nombre: "José Ñáñez" }));

    expect(obtenerNombre()).toBe("José Ñáñez");
  });

  it("devuelve null cuando el token no contiene el claim de nombre", () => {
    sessionStorage.setItem("token", crearTokenFalso({ [ROL_CLAIM]: "usuario" }));

    expect(obtenerNombre()).toBeNull();
  });

  it("devuelve null y no lanza excepción cuando el token está corrupto", () => {
    vi.spyOn(console, "error").mockImplementation(() => {});
    sessionStorage.setItem("token", "esto-no-es-un-jwt-valido");

    expect(obtenerNombre()).toBeNull();

    vi.restoreAllMocks();
  });
});

afterEach(() => {
  sessionStorage.clear();
});
