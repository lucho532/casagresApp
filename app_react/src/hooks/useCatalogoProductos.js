import { useEffect, useState } from "react";
import { obtenerProductos } from "../services/api";

// El catálogo asocia cada referencia con el nombre real del producto, para
// mostrar ese nombre en vez del código en el resto de la aplicación. Es una
// mejora de presentación, no un dato crítico: si falla la carga, cada
// pantalla simplemente sigue mostrando la referencia como antes.
export function useCatalogoProductos() {
  const [nombresPorReferencia, setNombresPorReferencia] = useState({});

  useEffect(() => {
    let activo = true;

    const cargarCatalogo = async () => {
      try {
        const datos = await obtenerProductos();
        const mapa = {};

        (datos.productos || []).forEach((producto) => {
          if (producto.referencia) {
            mapa[producto.referencia] = producto.descripcion;
          }
        });

        if (activo) {
          setNombresPorReferencia(mapa);
        }
      } catch (err) {
        console.error("No fue posible cargar el catálogo de productos:", err);
      }
    };

    cargarCatalogo();

    return () => {
      activo = false;
    };
  }, []);

  const obtenerNombreProducto = (referencia) =>
    nombresPorReferencia[referencia] || referencia;

  return { obtenerNombreProducto };
}
