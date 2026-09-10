import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import TarjetaPeriodo from "./TarjetaPeriodo";

const mesesDisponibles = [
  { mes: "2025-03-01" },
  { mes: "2025-02-01" },
  { mes: "2025-01-01" },
];

describe("TarjetaPeriodo", () => {
  it("muestra el mes formateado en español", () => {
    render(<TarjetaPeriodo mes="2025-03-01" />);

    expect(screen.getByText("marzo de 2025")).toBeInTheDocument();
  });

  it("usa la etiqueta y descripción por defecto cuando no se especifican", () => {
    render(<TarjetaPeriodo mes="2025-03-01" />);

    expect(screen.getByText("Periodo mostrado")).toBeInTheDocument();
    expect(
      screen.getByText("Datos proyectados para este mes"),
    ).toBeInTheDocument();
  });

  it("usa la etiqueta y descripción personalizadas cuando se indican", () => {
    render(
      <TarjetaPeriodo
        mes="2025-03-01"
        etiqueta="Periodo analizado"
        descripcion="Mes con datos analizados"
      />,
    );

    expect(screen.getByText("Periodo analizado")).toBeInTheDocument();
    expect(screen.getByText("Mes con datos analizados")).toBeInTheDocument();
  });

  it("muestra un guion cuando no hay mes", () => {
    render(<TarjetaPeriodo mes={null} />);

    expect(screen.getByText("-")).toBeInTheDocument();
  });

  it("sin mesesDisponibles u onCambiarMes, se muestra en modo de solo lectura", () => {
    render(<TarjetaPeriodo mes="2025-03-01" mesesDisponibles={mesesDisponibles} />);

    expect(screen.getByText("marzo de 2025")).toBeInTheDocument();
    expect(screen.queryByRole("combobox")).not.toBeInTheDocument();
  });

  it("con mesesDisponibles y onCambiarMes, muestra un selector con las opciones", () => {
    render(
      <TarjetaPeriodo
        mes="2025-02-01"
        etiqueta="Periodo analizado"
        mesesDisponibles={mesesDisponibles}
        onCambiarMes={vi.fn()}
      />,
    );

    const selector = screen.getByRole("combobox", { name: "Periodo analizado" });

    expect(selector).toHaveValue("2025-02-01");
    expect(screen.getByRole("option", { name: "marzo de 2025" })).toBeInTheDocument();
    expect(screen.getByRole("option", { name: "febrero de 2025" })).toBeInTheDocument();
    expect(screen.getByRole("option", { name: "enero de 2025" })).toBeInTheDocument();
  });

  it("al elegir otro mes en el selector, llama a onCambiarMes con el mes elegido", async () => {
    const onCambiarMes = vi.fn();
    const usuario = userEvent.setup();

    render(
      <TarjetaPeriodo
        mes="2025-02-01"
        mesesDisponibles={mesesDisponibles}
        onCambiarMes={onCambiarMes}
      />,
    );

    await usuario.selectOptions(screen.getByRole("combobox"), "2025-01-01");

    expect(onCambiarMes).toHaveBeenCalledWith("2025-01-01");
  });
});
