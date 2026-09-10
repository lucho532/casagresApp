import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import EstadoCargando from "./EstadoCargando";

describe("EstadoCargando", () => {
  it("muestra el mensaje recibido", () => {
    render(<EstadoCargando mensaje="Cargando catálogo..." />);

    expect(screen.getByText("Cargando catálogo...")).toBeInTheDocument();
  });

  it("usa solo la clase 'pagina' cuando no se indica una clase adicional", () => {
    const { container } = render(<EstadoCargando mensaje="Cargando..." />);

    expect(container.firstChild).toHaveClass("pagina");
    expect(container.firstChild.className).toBe("pagina");
  });

  it("agrega la clase adicional cuando se indica", () => {
    const { container } = render(
      <EstadoCargando mensaje="Cargando..." claseAdicional="productos" />,
    );

    expect(container.firstChild).toHaveClass("pagina", "productos");
  });
});
