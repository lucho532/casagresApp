import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import ControlActualizacion from "./ControlActualizacion";
import { actualizarDatos, obtenerEstadoActualizacion } from "../services/api";

vi.mock("../services/api", () => ({
  actualizarDatos: vi.fn(),
  obtenerEstadoActualizacion: vi.fn(),
}));

const estadoInactivo = {
  ejecutando: false,
  estado: "Sin actualizaciones en curso.",
  progreso: 0,
  ultimaActualizacion: null,
};

let ultimoRender;

function renderizar(props = {}) {
  ultimoRender = render(<ControlActualizacion {...props} />);
  return ultimoRender;
}

afterEach(() => {
  ultimoRender?.unmount();
  ultimoRender = null;
  vi.clearAllMocks();
});

describe("ControlActualizacion", () => {
  it("al montar, consulta el estado y muestra el botón habilitado si no hay nada en curso", async () => {
    obtenerEstadoActualizacion.mockResolvedValue(estadoInactivo);

    renderizar();

    await screen.findByText("Sin actualizaciones en curso.");

    expect(screen.getByRole("button", { name: /Actualizar datos/ })).toBeEnabled();
  });

  it("si al montar ya hay una actualización en curso, deshabilita el botón y muestra el progreso", async () => {
    obtenerEstadoActualizacion.mockResolvedValue({
      ejecutando: true,
      estado: "Procesando...",
      progreso: 40,
    });

    renderizar();

    await screen.findByText("Procesando...");

    expect(screen.getByRole("button", { name: /Actualizando/ })).toBeDisabled();
    expect(screen.getByText("40%")).toBeInTheDocument();
  });

  it("avisa al padre la última fecha de actualización cuando está disponible", async () => {
    const onUltimaActualizacion = vi.fn();
    obtenerEstadoActualizacion.mockResolvedValue({
      ...estadoInactivo,
      ultimaActualizacion: "2025-06-01T10:00:00Z",
    });

    renderizar({ onUltimaActualizacion });

    await waitFor(() =>
      expect(onUltimaActualizacion).toHaveBeenCalledWith("2025-06-01T10:00:00Z"),
    );
  });

  it("no avisa al padre cuando todavía no hay una última actualización", async () => {
    const onUltimaActualizacion = vi.fn();
    obtenerEstadoActualizacion.mockResolvedValue(estadoInactivo);

    renderizar({ onUltimaActualizacion });

    await screen.findByText("Sin actualizaciones en curso.");

    expect(onUltimaActualizacion).not.toHaveBeenCalled();
  });

  it("al hacer clic en Actualizar datos, llama a actualizarDatos con el horizonte escrito manualmente", async () => {
    obtenerEstadoActualizacion.mockResolvedValue(estadoInactivo);
    actualizarDatos.mockResolvedValue({});
    const usuario = userEvent.setup();

    renderizar();
    await screen.findByText("Sin actualizaciones en curso.");

    const campoHorizonte = screen.getByLabelText("Horizonte en meses:");
    await usuario.clear(campoHorizonte);
    await usuario.type(campoHorizonte, "6");
    await usuario.click(screen.getByRole("button", { name: /Actualizar datos/ }));

    expect(actualizarDatos).toHaveBeenCalledWith(6);
  });

  it("al presionar Enter en el campo de horizonte, ejecuta la actualización", async () => {
    obtenerEstadoActualizacion.mockResolvedValue(estadoInactivo);
    actualizarDatos.mockResolvedValue({});
    const usuario = userEvent.setup();

    renderizar();
    await screen.findByText("Sin actualizaciones en curso.");

    const campoHorizonte = screen.getByLabelText("Horizonte en meses:");
    await usuario.clear(campoHorizonte);
    await usuario.type(campoHorizonte, "6{Enter}");

    expect(actualizarDatos).toHaveBeenCalledWith(6);
  });

  it("al presionar Enter con un horizonte inválido, no ejecuta la actualización", async () => {
    obtenerEstadoActualizacion.mockResolvedValue(estadoInactivo);
    const usuario = userEvent.setup();

    renderizar();
    await screen.findByText("Sin actualizaciones en curso.");

    const campoHorizonte = screen.getByLabelText("Horizonte en meses:");
    await usuario.clear(campoHorizonte);
    await usuario.type(campoHorizonte, "{Enter}");

    expect(actualizarDatos).not.toHaveBeenCalled();
  });

  it("deshabilita el botón de actualizar mientras el horizonte esté vacío", async () => {
    obtenerEstadoActualizacion.mockResolvedValue(estadoInactivo);
    const usuario = userEvent.setup();

    renderizar();
    await screen.findByText("Sin actualizaciones en curso.");

    const campoHorizonte = screen.getByLabelText("Horizonte en meses:");
    await usuario.clear(campoHorizonte);

    expect(screen.getByRole("button", { name: /Actualizar datos/ })).toBeDisabled();
    expect(actualizarDatos).not.toHaveBeenCalled();
  });

  it("al salir del campo con un horizonte vacío, lo restablece a 1", async () => {
    obtenerEstadoActualizacion.mockResolvedValue(estadoInactivo);
    const usuario = userEvent.setup();

    renderizar();
    await screen.findByText("Sin actualizaciones en curso.");

    const campoHorizonte = screen.getByLabelText("Horizonte en meses:");
    await usuario.clear(campoHorizonte);
    await usuario.click(document.body);

    expect(campoHorizonte).toHaveValue(1);
    expect(screen.getByRole("button", { name: /Actualizar datos/ })).toBeEnabled();
  });

  it("cuando el backend responde 409, muestra que ya hay una actualización en curso", async () => {
    obtenerEstadoActualizacion.mockResolvedValue(estadoInactivo);
    actualizarDatos.mockRejectedValue({ response: { status: 409 } });

    const usuario = userEvent.setup();
    renderizar();
    await screen.findByText("Sin actualizaciones en curso.");

    await usuario.click(screen.getByRole("button", { name: /Actualizar datos/ }));

    await screen.findByText("Ya hay una actualización en curso.");
    expect(screen.getByRole("button", { name: /Actualizando/ })).toBeDisabled();
  });

  it("cuando falla con un error genérico, muestra el mensaje y vuelve a habilitar el botón", async () => {
    obtenerEstadoActualizacion.mockResolvedValue(estadoInactivo);
    actualizarDatos.mockRejectedValue({
      response: { data: { mensaje: "Falló el pipeline" } },
    });

    const usuario = userEvent.setup();
    renderizar();
    await screen.findByText("Sin actualizaciones en curso.");

    await usuario.click(screen.getByRole("button", { name: /Actualizar datos/ }));

    await screen.findByText("Falló el pipeline");
    expect(screen.getByRole("button", { name: /Actualizar datos/ })).toBeEnabled();
  });

  it("cuando el backend envía un detalle distinto del mensaje genérico, lo muestra también", async () => {
    obtenerEstadoActualizacion.mockResolvedValue(estadoInactivo);
    actualizarDatos.mockRejectedValue({
      response: {
        data: {
          mensaje: "Error durante la actualización.",
          detalle:
            "No se pudo guardar 'informe.xlsx': el archivo está abierto en otro programa (por ejemplo, Excel). Ciérralo e inténtalo de nuevo.",
        },
      },
    });

    const usuario = userEvent.setup();
    renderizar();
    await screen.findByText("Sin actualizaciones en curso.");

    await usuario.click(screen.getByRole("button", { name: /Actualizar datos/ }));

    await screen.findByText("Error durante la actualización.");
    expect(
      screen.getByText(/el archivo está abierto en otro programa/),
    ).toBeInTheDocument();
  });

  it("cuando el detalle del backend es igual al mensaje, no lo repite", async () => {
    obtenerEstadoActualizacion.mockResolvedValue(estadoInactivo);
    actualizarDatos.mockRejectedValue({
      response: {
        data: { mensaje: "Falló el pipeline", detalle: "Falló el pipeline" },
      },
    });

    const usuario = userEvent.setup();
    renderizar();
    await screen.findByText("Sin actualizaciones en curso.");

    await usuario.click(screen.getByRole("button", { name: /Actualizar datos/ }));

    expect(await screen.findAllByText("Falló el pipeline")).toHaveLength(1);
  });

  it("cuando falla sin mensaje del backend, usa el texto de error genérico", async () => {
    obtenerEstadoActualizacion.mockResolvedValue(estadoInactivo);
    actualizarDatos.mockRejectedValue(new Error("network error"));

    const usuario = userEvent.setup();
    renderizar();
    await screen.findByText("Sin actualizaciones en curso.");

    await usuario.click(screen.getByRole("button", { name: /Actualizar datos/ }));

    await screen.findByText("Error durante la actualización.");
  });

  it("no muestra la barra de progreso cuando no hay ninguna actualización en curso", async () => {
    obtenerEstadoActualizacion.mockResolvedValue(estadoInactivo);

    renderizar();

    await screen.findByText("Sin actualizaciones en curso.");

    expect(screen.queryByText(/%$/)).not.toBeInTheDocument();
  });
});
