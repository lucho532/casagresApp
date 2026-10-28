import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import CuentaPendiente from "./CuentaPendiente";

describe("CuentaPendiente", () => {
  it("muestra el mensaje de cuenta pendiente de aprobación", () => {
    render(<CuentaPendiente onCerrarSesion={vi.fn()} />);

    expect(
      screen.getByText("Cuenta pendiente de aprobación"),
    ).toBeInTheDocument();
  });

  it("al hacer clic en 'Cerrar sesión', llama a onCerrarSesion", async () => {
    const onCerrarSesion = vi.fn();
    const usuario = userEvent.setup();

    render(<CuentaPendiente onCerrarSesion={onCerrarSesion} />);

    await usuario.click(screen.getByRole("button", { name: "Cerrar sesión" }));

    expect(onCerrarSesion).toHaveBeenCalledTimes(1);
  });
});
