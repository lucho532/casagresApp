import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import MensajeError from "./MensajeError";

describe("MensajeError", () => {
  it("muestra el mensaje recibido", () => {
    render(<MensajeError mensaje="No fue posible cargar los datos." />);

    expect(
      screen.getByText("No fue posible cargar los datos."),
    ).toBeInTheDocument();
  });

  it("usa solo la clase 'pagina' cuando no se indica una clase adicional", () => {
    const { container } = render(<MensajeError mensaje="Error" />);

    expect(container.firstChild.className).toBe("pagina");
  });

  it("agrega la clase adicional cuando se indica", () => {
    const { container } = render(
      <MensajeError mensaje="Error" claseAdicional="productos" />,
    );

    expect(container.firstChild).toHaveClass("pagina", "productos");
  });
});
