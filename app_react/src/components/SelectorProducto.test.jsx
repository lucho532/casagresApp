import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import SelectorProducto from "./SelectorProducto";

const productos = [
  { referencia: "REF1" },
  { referencia: "REF2" },
  { referencia: "OTRA3" },
];

describe("SelectorProducto", () => {
  it("muestra una opción por cada producto recibido", () => {
    render(
      <SelectorProducto
        productos={productos}
        valorSeleccionado="REF1"
        onSeleccionar={vi.fn()}
      />,
    );

    expect(screen.getAllByRole("option")).toHaveLength(3);
  });

  it("usa 'Producto' como etiqueta por defecto", () => {
    render(
      <SelectorProducto
        productos={productos}
        valorSeleccionado="REF1"
        onSeleccionar={vi.fn()}
      />,
    );

    expect(screen.getByText("Producto")).toBeInTheDocument();
  });

  it("usa la etiqueta personalizada cuando se indica", () => {
    render(
      <SelectorProducto
        productos={productos}
        valorSeleccionado="REF1"
        onSeleccionar={vi.fn()}
        etiqueta="Referencia"
      />,
    );

    expect(screen.getByText("Referencia")).toBeInTheDocument();
  });

  it("llama a onSeleccionar con la referencia elegida en el desplegable", async () => {
    const onSeleccionar = vi.fn();
    const usuario = userEvent.setup();

    render(
      <SelectorProducto
        productos={productos}
        valorSeleccionado="REF1"
        onSeleccionar={onSeleccionar}
      />,
    );

    await usuario.selectOptions(screen.getByRole("combobox"), "REF2");

    expect(onSeleccionar).toHaveBeenCalledWith("REF2");
  });

  it("el campo de búsqueda está oculto inicialmente", () => {
    render(
      <SelectorProducto
        productos={productos}
        valorSeleccionado="REF1"
        onSeleccionar={vi.fn()}
      />,
    );

    expect(
      screen.queryByPlaceholderText("Buscar referencia..."),
    ).not.toBeInTheDocument();
  });

  it("el botón de lupa muestra y oculta el campo de búsqueda", async () => {
    const usuario = userEvent.setup();

    render(
      <SelectorProducto
        productos={productos}
        valorSeleccionado="REF1"
        onSeleccionar={vi.fn()}
      />,
    );

    const boton = screen.getByTitle("Buscar referencia");

    await usuario.click(boton);
    expect(
      screen.getByPlaceholderText("Buscar referencia..."),
    ).toBeInTheDocument();

    await usuario.click(boton);
    expect(
      screen.queryByPlaceholderText("Buscar referencia..."),
    ).not.toBeInTheDocument();
  });

  it("filtra las opciones del desplegable según el texto buscado", async () => {
    const usuario = userEvent.setup();

    render(
      <SelectorProducto
        productos={productos}
        valorSeleccionado="REF1"
        onSeleccionar={vi.fn()}
      />,
    );

    await usuario.click(screen.getByTitle("Buscar referencia"));
    await usuario.type(screen.getByPlaceholderText("Buscar referencia..."), "ref");

    // "REF1" y "REF2" contienen "ref" (sin distinguir mayúsculas); "OTRA3" no.
    expect(screen.getAllByRole("option")).toHaveLength(2);
  });

  it("muestra el contador de resultados en singular y en plural", async () => {
    const usuario = userEvent.setup();

    render(
      <SelectorProducto
        productos={productos}
        valorSeleccionado="REF1"
        onSeleccionar={vi.fn()}
      />,
    );

    await usuario.click(screen.getByTitle("Buscar referencia"));
    const input = screen.getByPlaceholderText("Buscar referencia...");

    await usuario.type(input, "ref");
    expect(screen.getByText("2 resultados")).toBeInTheDocument();

    await usuario.clear(input);
    await usuario.type(input, "OTRA3");
    expect(screen.getByText("1 resultado")).toBeInTheDocument();
  });

  it("al presionar Enter selecciona el primer resultado filtrado y cierra la búsqueda", async () => {
    const onSeleccionar = vi.fn();
    const usuario = userEvent.setup();

    render(
      <SelectorProducto
        productos={productos}
        valorSeleccionado="REF1"
        onSeleccionar={onSeleccionar}
      />,
    );

    await usuario.click(screen.getByTitle("Buscar referencia"));
    await usuario.type(
      screen.getByPlaceholderText("Buscar referencia..."),
      "REF2{Enter}",
    );

    expect(onSeleccionar).toHaveBeenCalledWith("REF2");
    expect(
      screen.queryByPlaceholderText("Buscar referencia..."),
    ).not.toBeInTheDocument();
  });

  it("Enter sin resultados no llama a onSeleccionar", async () => {
    const onSeleccionar = vi.fn();
    const usuario = userEvent.setup();

    render(
      <SelectorProducto
        productos={productos}
        valorSeleccionado="REF1"
        onSeleccionar={onSeleccionar}
      />,
    );

    await usuario.click(screen.getByTitle("Buscar referencia"));
    await usuario.type(
      screen.getByPlaceholderText("Buscar referencia..."),
      "no-existe{Enter}",
    );

    expect(onSeleccionar).not.toHaveBeenCalled();
  });
});
