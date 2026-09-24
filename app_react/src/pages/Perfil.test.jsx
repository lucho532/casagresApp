import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import Perfil from "./Perfil";
import { obtenerUrlCompleta, subirFotoPerfil } from "../services/api";

vi.mock("../services/api", () => ({
  obtenerUrlCompleta: vi.fn((ruta) => (ruta ? `https://api.ejemplo.com${ruta}` : ruta)),
  subirFotoPerfil: vi.fn(),
}));

function crearArchivo(nombre, contentType, tamanoBytes = 100) {
  return new File([new Uint8Array(tamanoBytes)], nombre, { type: contentType });
}

const perfilBase = {
  id: 1,
  nombre: "Juan Pérez",
  email: "juan@ejemplo.com",
  rol: "usuario",
};

beforeEach(() => {
  vi.clearAllMocks();
});

describe("Perfil", () => {
  it("muestra el nombre, correo y rol del usuario", () => {
    render(<Perfil perfil={perfilBase} />);

    expect(screen.getByText("Juan Pérez")).toBeInTheDocument();
    expect(screen.getByText("juan@ejemplo.com")).toBeInTheDocument();
    expect(screen.getByText("Usuario")).toBeInTheDocument();
  });

  it("muestra 'Administrador' cuando el rol es admin", () => {
    render(<Perfil perfil={{ ...perfilBase, rol: "admin" }} />);

    expect(screen.getByText("Administrador")).toBeInTheDocument();
  });

  it("muestra un guion en los campos cuando no hay perfil", () => {
    render(<Perfil perfil={null} />);

    expect(screen.getAllByText("-")).toHaveLength(2);
  });

  describe("avatar", () => {
    it("muestra las iniciales cuando no hay foto de perfil", () => {
      render(<Perfil perfil={perfilBase} />);

      expect(screen.getByText("JP")).toBeInTheDocument();
      expect(screen.queryByAltText("Foto de perfil")).not.toBeInTheDocument();
    });

    it("muestra la foto cuando está disponible", () => {
      render(<Perfil perfil={{ ...perfilBase, fotoUrl: "/api/auth/foto-perfil/1" }} />);

      const imagen = screen.getByAltText("Foto de perfil");
      expect(imagen).toHaveAttribute("src", "https://api.ejemplo.com/api/auth/foto-perfil/1");
    });

    it("si la foto falla al cargar, vuelve a mostrar las iniciales", () => {
      render(<Perfil perfil={{ ...perfilBase, fotoUrl: "/api/auth/foto-perfil/1" }} />);

      fireEvent.error(screen.getByAltText("Foto de perfil"));

      expect(screen.getByText("JP")).toBeInTheDocument();
    });
  });

  describe("ampliar la foto", () => {
    const perfilConFoto = { ...perfilBase, fotoUrl: "/api/auth/foto-perfil/1" };

    it("al hacer clic en la foto, la muestra ampliada", async () => {
      const usuario = userEvent.setup();
      render(<Perfil perfil={perfilConFoto} />);

      await usuario.click(screen.getByAltText("Foto de perfil"));

      expect(screen.getByAltText("Foto de perfil ampliada")).toHaveAttribute(
        "src",
        "https://api.ejemplo.com/api/auth/foto-perfil/1",
      );
    });

    it("con Enter en el avatar, también la amplía", async () => {
      const usuario = userEvent.setup();
      render(<Perfil perfil={perfilConFoto} />);

      screen.getByAltText("Foto de perfil").closest('[role="button"]').focus();
      await usuario.keyboard("{Enter}");

      expect(screen.getByAltText("Foto de perfil ampliada")).toBeInTheDocument();
    });

    it("al hacer clic en las iniciales (sin foto), no amplía nada", async () => {
      const usuario = userEvent.setup();
      render(<Perfil perfil={perfilBase} />);

      await usuario.click(screen.getByText("JP"));

      expect(screen.queryByAltText("Foto de perfil ampliada")).not.toBeInTheDocument();
    });

    it("al hacer clic en el fondo, cierra la vista ampliada", async () => {
      const usuario = userEvent.setup();
      render(<Perfil perfil={perfilConFoto} />);

      await usuario.click(screen.getByAltText("Foto de perfil"));
      await usuario.click(screen.getByAltText("Foto de perfil ampliada"));

      expect(screen.queryByAltText("Foto de perfil ampliada")).not.toBeInTheDocument();
    });

    it("al hacer clic en el botón de cerrar, cierra la vista ampliada", async () => {
      const usuario = userEvent.setup();
      render(<Perfil perfil={perfilConFoto} />);

      await usuario.click(screen.getByAltText("Foto de perfil"));
      await usuario.click(screen.getByRole("button", { name: "Cerrar" }));

      expect(screen.queryByAltText("Foto de perfil ampliada")).not.toBeInTheDocument();
    });

    it("con Escape, cierra la vista ampliada", async () => {
      const usuario = userEvent.setup();
      render(<Perfil perfil={perfilConFoto} />);

      await usuario.click(screen.getByAltText("Foto de perfil"));
      await usuario.keyboard("{Escape}");

      expect(screen.queryByAltText("Foto de perfil ampliada")).not.toBeInTheDocument();
    });
  });

  describe("subir foto de perfil", () => {
    it("al elegir una imagen válida, la sube y avisa al padre con la nueva URL", async () => {
      const onFotoActualizada = vi.fn();
      subirFotoPerfil.mockResolvedValue({ fotoUrl: "/api/auth/foto-perfil/1?v=123" });
      const usuario = userEvent.setup();

      const { container } = render(
        <Perfil perfil={perfilBase} onFotoActualizada={onFotoActualizada} />,
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
      const { container } = render(<Perfil perfil={perfilBase} />);

      const input = container.querySelector('input[type="file"]');
      const archivo = crearArchivo("foto.gif", "image/gif");

      // userEvent.upload filtra según el atributo "accept" del input; se usa
      // fireEvent para simular el caso límite en que igual llega un archivo
      // no permitido (por ejemplo, arrastrándolo) y probar la validación
      // propia del componente como segunda barrera.
      fireEvent.change(input, { target: { files: [archivo] } });

      expect(await screen.findByText("Usa una imagen JPG, PNG o WEBP.")).toBeInTheDocument();
      expect(subirFotoPerfil).not.toHaveBeenCalled();
    });

    it("rechaza una imagen mayor a 3 MB sin llamar al backend", async () => {
      const usuario = userEvent.setup();
      const { container } = render(<Perfil perfil={perfilBase} />);

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
      const { container } = render(<Perfil perfil={perfilBase} />);

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
      const { container } = render(<Perfil perfil={perfilBase} />);

      const input = container.querySelector('input[type="file"]');
      await usuario.upload(input, crearArchivo("foto.png", "image/png"));

      expect(screen.getByRole("button", { name: "Subiendo..." })).toBeDisabled();

      resolverSubida({ fotoUrl: "/api/auth/foto-perfil/1" });

      await waitFor(() =>
        expect(screen.getByRole("button", { name: "Cambiar foto" })).toBeEnabled(),
      );
    });

    it("al hacer clic en 'Cambiar foto', abre el selector de archivos", async () => {
      const usuario = userEvent.setup();
      const { container } = render(<Perfil perfil={perfilBase} />);

      const input = container.querySelector('input[type="file"]');
      const clicEnInput = vi.spyOn(input, "click");

      await usuario.click(screen.getByRole("button", { name: "Cambiar foto" }));

      expect(clicEnInput).toHaveBeenCalledTimes(1);
    });
  });
});
