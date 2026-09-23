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

  // Para mostrar "Nombre · CÓDIGO" junto al nombre. Si el catálogo aún no
  // cargó (o la referencia no aparece en él), obtenerNombreProducto ya cae
  // de vuelta al código, así que aquí se evita repetirlo dos veces.
  const obtenerNombreConCodigo = (referencia) => {
    const nombre = obtenerNombreProducto(referencia);

    return nombre === referencia ? nombre : `${nombre} · ${referencia}`;
  };

  return { obtenerNombreProducto, obtenerNombreConCodigo };
}
