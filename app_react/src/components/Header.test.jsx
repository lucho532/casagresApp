import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import Header from "./Header";
import { obtenerNombre, obtenerRol } from "../utils/auth";
import { obtenerUrlCompleta, subirFotoPerfil } from "../services/api";

vi.mock("../utils/auth", () => ({
  obtenerNombre: vi.fn(),
  obtenerRol: vi.fn(),
}));

vi.mock("../services/api", () => ({
  obtenerUrlCompleta: vi.fn((ruta) => (ruta ? `https://api.ejemplo.com${ruta}` : ruta)),
  subirFotoPerfil: vi.fn(),
}));

function crearArchivo(nombre, contentType, tamanoBytes = 100) {
  const archivo = new File([new Uint8Array(tamanoBytes)], nombre, { type: contentType });
  return archivo;
}

beforeEach(() => {
  obtenerNombre.mockReturnValue(null);
  obtenerRol.mockReturnValue(null);
  vi.clearAllMocks();
});

describe("Header", () => {
  it("muestra el título correspondiente a la página actual", () => {
    render(<Header paginaActual="tendencias" />);

    expect(
      screen.getByRole("heading", { name: "Tendencias de compra" }),
    ).toBeInTheDocument();
  });

  it("muestra 'CASAGRES' cuando la página no tiene título definido", () => {
    render(<Header paginaActual="pagina-desconocida" />);

    expect(screen.getByRole("heading", { name: "CASAGRES" })).toBeInTheDocument();
  });

  it("muestra el nombre del usuario cuando está disponible", () => {
    obtenerNombre.mockReturnValue("Juan Pérez");

    render(<Header paginaActual="inicio" />);

    expect(screen.getByText("Juan Pérez")).toBeInTheDocument();
  });

  it("muestra 'Usuario' cuando no hay nombre disponible", () => {
    render(<Header paginaActual="inicio" />);

    expect(
      screen.getByText("Usuario", { selector: ".header-usuario-nombre" }),
    ).toBeInTheDocument();
  });

  it("muestra 'Administrador' cuando el rol es admin", () => {
    obtenerRol.mockReturnValue("admin");

    render(<Header paginaActual="inicio" />);

    expect(screen.getByText("Administrador")).toBeInTheDocument();
  });

  it("muestra 'Usuario' como rol cuando no es admin", () => {
    obtenerRol.mockReturnValue("usuario");

    render(<Header paginaActual="inicio" />);

    expect(
      screen.getByText("Usuario", { selector: ".header-usuario-rol" }),
    ).toBeInTheDocument();
  });

  it("no muestra la última actualización cuando no se recibe fecha", () => {
    render(<Header paginaActual="inicio" ultimaActualizacion={null} />);

    expect(screen.queryByText(/Última actualización/)).not.toBeInTheDocument();
  });

  it("muestra la última actualización cuando la fecha es válida", () => {
    render(
      <Header paginaActual="inicio" ultimaActualizacion="2025-06-01T10:00:00Z" />,
    );

    expect(screen.getByText(/Última actualización:/)).toBeInTheDocument();
  });

  it("no muestra la última actualización cuando la fecha es inválida", () => {
    render(<Header paginaActual="inicio" ultimaActualizacion="fecha-no-valida" />);

    expect(screen.queryByText(/Última actualización/)).not.toBeInTheDocument();
  });

  describe("avatar", () => {
    it("muestra las iniciales cuando no hay foto de perfil", () => {
      obtenerNombre.mockReturnValue("Juan Pérez");

      render(<Header paginaActual="inicio" />);

      expect(screen.getByText("JP")).toBeInTheDocument();
      expect(screen.queryByAltText("Foto de perfil")).not.toBeInTheDocument();
    });

    it("muestra el ícono genérico cuando no hay nombre ni foto", () => {
      render(<Header paginaActual="inicio" />);

      expect(screen.getByText("👤")).toBeInTheDocument();
    });

    it("muestra la foto de perfil cuando está disponible", () => {
      render(<Header paginaActual="inicio" fotoUrl="/api/auth/foto-perfil/1" />);

      const imagen = screen.getByAltText("Foto de perfil");
      expect(imagen).toHaveAttribute("src", "https://api.ejemplo.com/api/auth/foto-perfil/1");
      expect(obtenerUrlCompleta).toHaveBeenCalledWith("/api/auth/foto-perfil/1");
    });

    it("si la foto falla al cargar, vuelve a mostrar las iniciales", () => {
      obtenerNombre.mockReturnValue("Juan Pérez");

      render(<Header paginaActual="inicio" fotoUrl="/api/auth/foto-perfil/1" />);

      fireEvent.error(screen.getByAltText("Foto de perfil"));

      expect(screen.getByText("JP")).toBeInTheDocument();
      expect(screen.queryByAltText("Foto de perfil")).not.toBeInTheDocument();
    });
  });

  describe("subir foto de perfil", () => {
    it("al elegir una imagen válida, la sube y avisa al padre con la nueva URL", async () => {
      const onFotoActualizada = vi.fn();
      subirFotoPerfil.mockResolvedValue({ fotoUrl: "/api/auth/foto-perfil/1?v=123" });
      const usuario = userEvent.setup();

      const { container } = render(
        <Header paginaActual="inicio" onFotoActualizada={onFotoActualizada} />,
      );

      const input = container.querySelector('input[type="file"]');
      const archivo = crearArchivo("foto.jpg", "image/jpeg");

      await usuario.upload(input, archivo);

      await waitFor(() =>
        expect(onFotoActualizada).toHaveBeenCalledWith("/api/auth/foto-perfil/1?v=123"),
      );
      expect(subirFotoPerfil).toHaveBeenCalledWith(archivo);
    });

    it("rechaza un formato no soportado sin llamar al backend", async () => {
      const { container } = render(<Header paginaActual="inicio" />);

      const input = container.querySelector('input[type="file"]');
      const archivo = crearArchivo("foto.gif", "image/gif");

      // userEvent.upload filtra los archivos según el atributo "accept" del
      // input, así que un .gif nunca llegaría a disparar el evento (el
      // selector nativo del navegador ya lo habría bloqueado). Se usa
      // fireEvent para simular el caso límite en que sí llega uno (por
      // ejemplo, arrastrando el archivo), y así probar la validación propia
      // del componente como una segunda barrera.
      fireEvent.change(input, { target: { files: [archivo] } });

      expect(await screen.findByText("Usa una imagen JPG, PNG o WEBP.")).toBeInTheDocument();
      expect(subirFotoPerfil).not.toHaveBeenCalled();
    });

    it("rechaza una imagen mayor a 3 MB sin llamar al backend", async () => {
      const usuario = userEvent.setup();
      const { container } = render(<Header paginaActual="inicio" />);

      const input = container.querySelector('input[type="file"]');
      const archivo = crearArchivo("foto.jpg", "image/jpeg", 3 * 1024 * 1024 + 1);

      await usuario.upload(input, archivo);

      expect(
        await screen.findByText("La imagen no puede superar los 3 MB."),
      ).toBeInTheDocument();
      expect(subirFotoPerfil).not.toHaveBeenCalled();
    });

    it("si el backend rechaza la imagen, muestra el mensaje de error", async () => {
      subirFotoPerfil.mockRejectedValue({
        response: { data: { mensaje: "La imagen no pudo procesarse." } },
      });
      const usuario = userEvent.setup();
      const { container } = render(<Header paginaActual="inicio" />);

      const input = container.querySelector('input[type="file"]');
      await usuario.upload(input, crearArchivo("foto.png", "image/png"));

      expect(
        await screen.findByText("La imagen no pudo procesarse."),
      ).toBeInTheDocument();
    });

    it("deshabilita el botón de cambiar foto mientras se sube", async () => {
      let resolverSubida;
      subirFotoPerfil.mockReturnValue(
        new Promise((resolve) => {
          resolverSubida = resolve;
        }),
      );
      const usuario = userEvent.setup();
      const { container } = render(<Header paginaActual="inicio" />);

      const input = container.querySelector('input[type="file"]');
      await usuario.upload(input, crearArchivo("foto.png", "image/png"));

      expect(
        screen.getByRole("button", { name: "Cambiar foto de perfil" }),
      ).toBeDisabled();

      resolverSubida({ fotoUrl: "/api/auth/foto-perfil/1" });

      await waitFor(() =>
        expect(
          screen.getByRole("button", { name: "Cambiar foto de perfil" }),
        ).toBeEnabled(),
      );
    });
  });
});
