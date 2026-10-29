import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import Administracion from "./Administracion";
import {
  cambiarEstado,
  cambiarRol,
  eliminarUsuario,
  obtenerUsuarios,
} from "../services/api";

vi.mock("../services/api", () => ({
  obtenerUsuarios: vi.fn(),
  cambiarRol: vi.fn(),
  cambiarEstado: vi.fn(),
  eliminarUsuario: vi.fn(),
}));

const usuarios = [
  {
    id: 1,
    usuario: "jperez",
    nombre: "Juan Pérez",
    email: "juan@ejemplo.com",
    rol: "usuario",
    activo: true,
    fechaCreacion: "2025-01-01T00:00:00Z",
  },
  {
    id: 2,
    usuario: "admin1",
    nombre: "Admin Uno",
    email: "admin@ejemplo.com",
    rol: "admin",
    activo: false,
    fechaCreacion: "2025-02-01T00:00:00Z",
  },
];

const usuarioPendiente = {
  id: 3,
  usuario: "nuevo1",
  nombre: "Usuario Nuevo",
  email: "nuevo@ejemplo.com",
  rol: "pendiente",
  activo: true,
  fechaCreacion: "2025-03-01T00:00:00Z",
};

afterEach(() => {
  vi.restoreAllMocks();
});

describe("Administracion", () => {
  it("muestra el estado de carga mientras llega la respuesta", () => {
    obtenerUsuarios.mockReturnValue(new Promise(() => {}));

    render(<Administracion />);

    expect(screen.getByText("Cargando usuarios...")).toBeInTheDocument();
  });

  it("muestra un mensaje específico cuando el usuario no tiene permisos (403)", async () => {
    obtenerUsuarios.mockRejectedValue({ response: { status: 403 } });
    vi.spyOn(console, "error").mockImplementation(() => {});

    render(<Administracion />);

    expect(
      await screen.findByText("No tienes permisos para acceder a esta sección."),
    ).toBeInTheDocument();
  });

  it("muestra un mensaje genérico ante otros errores", async () => {
    obtenerUsuarios.mockRejectedValue(new Error("network error"));
    vi.spyOn(console, "error").mockImplementation(() => {});

    render(<Administracion />);

    expect(
      await screen.findByText("No fue posible cargar los usuarios."),
    ).toBeInTheDocument();
  });

  it("muestra la tabla de usuarios y el contador en plural", async () => {
    obtenerUsuarios.mockResolvedValue(usuarios);

    render(<Administracion />);

    expect(await screen.findByText("jperez")).toBeInTheDocument();
    expect(screen.getByText("admin1")).toBeInTheDocument();
    expect(screen.getByText("2 usuarios")).toBeInTheDocument();
  });

  it("muestra el contador en singular cuando hay un solo usuario", async () => {
    obtenerUsuarios.mockResolvedValue([usuarios[0]]);

    render(<Administracion />);

    expect(await screen.findByText("1 usuario")).toBeInTheDocument();
  });

  it("cambia el rol del usuario cuando se confirma la acción", async () => {
    obtenerUsuarios.mockResolvedValue(usuarios);
    cambiarRol.mockResolvedValue({});
    vi.spyOn(window, "confirm").mockReturnValue(true);
    const usuarioEvento = userEvent.setup();

    render(<Administracion />);
    await screen.findByText("jperez");

    const fila = screen.getByText("jperez").closest("tr");
    await usuarioEvento.click(within(fila).getByRole("button", { name: "Hacer admin" }));

    expect(cambiarRol).toHaveBeenCalledWith(1, "admin");
    expect(await within(fila).findByText("Administrador")).toBeInTheDocument();
  });

  it("no cambia el rol si el usuario cancela la confirmación", async () => {
    obtenerUsuarios.mockResolvedValue(usuarios);
    vi.spyOn(window, "confirm").mockReturnValue(false);
    const usuarioEvento = userEvent.setup();

    render(<Administracion />);
    await screen.findByText("jperez");

    const fila = screen.getByText("jperez").closest("tr");
    await usuarioEvento.click(within(fila).getByRole("button", { name: "Hacer admin" }));

    expect(cambiarRol).not.toHaveBeenCalled();
  });

  it("muestra una alerta cuando falla el cambio de rol", async () => {
    obtenerUsuarios.mockResolvedValue(usuarios);
    cambiarRol.mockRejectedValue({ response: { status: 403 } });
    vi.spyOn(window, "confirm").mockReturnValue(true);
    const alerta = vi.spyOn(window, "alert").mockImplementation(() => {});
    vi.spyOn(console, "error").mockImplementation(() => {});
    const usuarioEvento = userEvent.setup();

    render(<Administracion />);
    await screen.findByText("jperez");

    const fila = screen.getByText("jperez").closest("tr");
    await usuarioEvento.click(within(fila).getByRole("button", { name: "Hacer admin" }));

    expect(alerta).toHaveBeenCalledWith("No tienes permisos para realizar esta acción.");
  });

  it("cambia el estado del usuario cuando se confirma la acción", async () => {
    obtenerUsuarios.mockResolvedValue(usuarios);
    cambiarEstado.mockResolvedValue({});
    vi.spyOn(window, "confirm").mockReturnValue(true);
    const usuarioEvento = userEvent.setup();

    render(<Administracion />);
    await screen.findByText("jperez");

    const fila = screen.getByText("jperez").closest("tr");
    await usuarioEvento.click(within(fila).getByRole("button", { name: "Desactivar" }));

    expect(cambiarEstado).toHaveBeenCalledWith(1, false);
    expect(await within(fila).findByText("Inactivo")).toBeInTheDocument();
  });

  it("elimina el usuario cuando se confirma la acción", async () => {
    obtenerUsuarios.mockResolvedValue(usuarios);
    eliminarUsuario.mockResolvedValue({});
    vi.spyOn(window, "confirm").mockReturnValue(true);
    const usuarioEvento = userEvent.setup();

    render(<Administracion />);
    await screen.findByText("jperez");

    const fila = screen.getByText("jperez").closest("tr");
    await usuarioEvento.click(within(fila).getByRole("button", { name: "Eliminar" }));

    expect(eliminarUsuario).toHaveBeenCalledWith(1);
    expect(screen.queryByText("jperez")).not.toBeInTheDocument();
    expect(screen.getByText("1 usuario")).toBeInTheDocument();
  });

  it("no elimina el usuario si se cancela la confirmación", async () => {
    obtenerUsuarios.mockResolvedValue(usuarios);
    vi.spyOn(window, "confirm").mockReturnValue(false);
    const usuarioEvento = userEvent.setup();

    render(<Administracion />);
    await screen.findByText("jperez");

    const fila = screen.getByText("jperez").closest("tr");
    await usuarioEvento.click(within(fila).getByRole("button", { name: "Eliminar" }));

    expect(eliminarUsuario).not.toHaveBeenCalled();
    expect(screen.getByText("jperez")).toBeInTheDocument();
  });

  it("muestra una alerta cuando falla la eliminación", async () => {
    obtenerUsuarios.mockResolvedValue(usuarios);
    eliminarUsuario.mockRejectedValue({
      response: { status: 400, data: { mensaje: "No puedes eliminar tu propio usuario." } },
    });
    vi.spyOn(window, "confirm").mockReturnValue(true);
    const alerta = vi.spyOn(window, "alert").mockImplementation(() => {});
    vi.spyOn(console, "error").mockImplementation(() => {});
    const usuarioEvento = userEvent.setup();

    render(<Administracion />);
    await screen.findByText("jperez");

    const fila = screen.getByText("jperez").closest("tr");
    await usuarioEvento.click(within(fila).getByRole("button", { name: "Eliminar" }));

    expect(alerta).toHaveBeenCalledWith("No puedes eliminar tu propio usuario.");
    expect(screen.getByText("jperez")).toBeInTheDocument();
  });

  // ============================================================
  // USUARIOS PENDIENTES DE APROBACIÓN
  // ============================================================

  it("muestra el badge 'Pendiente de aprobación' para un usuario nuevo", async () => {
    obtenerUsuarios.mockResolvedValue([usuarioPendiente]);

    render(<Administracion />);

    expect(await screen.findByText("Pendiente de aprobación")).toBeInTheDocument();
  });

  it("ofrece 'Aprobar acceso' en vez del botón normal de cambiar rol", async () => {
    obtenerUsuarios.mockResolvedValue([usuarioPendiente]);

    render(<Administracion />);

    await screen.findByText("nuevo1");

    expect(
      screen.getByRole("button", { name: "Aprobar acceso" }),
    ).toBeInTheDocument();
  });

  it("al aprobar un usuario pendiente, le asigna el rol 'usuario'", async () => {
    obtenerUsuarios.mockResolvedValue([usuarioPendiente]);
    cambiarRol.mockResolvedValue({});
    vi.spyOn(window, "confirm").mockReturnValue(true);
    const usuarioEvento = userEvent.setup();

    render(<Administracion />);
    await screen.findByText("nuevo1");

    await usuarioEvento.click(
      screen.getByRole("button", { name: "Aprobar acceso" }),
    );

    expect(cambiarRol).toHaveBeenCalledWith(3, "usuario");

    const fila = screen.getByText("nuevo1").closest("tr");
    expect(await within(fila).findByText("Usuario")).toBeInTheDocument();
  });

  it("no aprueba al usuario si se cancela la confirmación", async () => {
    obtenerUsuarios.mockResolvedValue([usuarioPendiente]);
    vi.spyOn(window, "confirm").mockReturnValue(false);
    const usuarioEvento = userEvent.setup();

    render(<Administracion />);
    await screen.findByText("nuevo1");

    await usuarioEvento.click(
      screen.getByRole("button", { name: "Aprobar acceso" }),
    );

    expect(cambiarRol).not.toHaveBeenCalled();
    expect(screen.getByText("Pendiente de aprobación")).toBeInTheDocument();
  });
});
