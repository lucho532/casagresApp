import { useEffect, useMemo, useState } from "react";
import { obtenerProductos } from "../services/api";

function Productos() {
  const [productos, setProductos] = useState([]);
  const [busqueda, setBusqueda] = useState("");
  const [productoSeleccionado, setProductoSeleccionado] = useState(null);

  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState("");

  // =========================================
  // CARGAR PRODUCTOS
  // =========================================

  useEffect(() => {
    const cargarProductos = async () => {
      try {
        setCargando(true);
        setError("");

        const datos = await obtenerProductos();

        setProductos(datos.productos || []);
      } catch (err) {
        console.error(err);

        setError("No fue posible obtener el catálogo de productos.");
      } finally {
        setCargando(false);
      }
    };

    cargarProductos();
  }, []);

  // =========================================
  // FILTRAR PRODUCTOS
  // =========================================

  const productosFiltrados = useMemo(() => {
    const texto = busqueda.trim().toLowerCase();

    if (!texto) {
      return productos;
    }

    return productos.filter((producto) => {
      const referencia = producto.referencia?.toLowerCase() || "";

      const descripcion = producto.descripcion?.toLowerCase() || "";

      return referencia.includes(texto) || descripcion.includes(texto);
    });
  }, [productos, busqueda]);

  // =========================================
  // ESTADO DE CARGA
  // =========================================

  if (cargando) {
    return (
      <div className="pagina productos">
        <div className="estado-cargando">Cargando catálogo de productos...</div>
      </div>
    );
  }

  // =========================================
  // ERROR
  // =========================================

  if (error) {
    return (
      <div className="pagina productos">
        <div className="mensaje-error">{error}</div>
      </div>
    );
  }

  return (
    <div className="pagina productos">
      {/* =====================================
          ENCABEZADO
      ====================================== */}

      <div className="pagina-encabezado">
        <div>
          <h2>Catálogo de productos</h2>

          <p>
            Consulta rápidamente a qué producto corresponde cada referencia.
          </p>
        </div>
      </div>

      {/* =====================================
          BUSCADOR
      ====================================== */}

      <div className="productos-buscador">
        <div className="productos-buscador-input">
          <span className="productos-icono-busqueda">🔍</span>

          <input
            type="text"
            placeholder="Buscar por referencia o nombre del producto..."
            value={busqueda}
            onChange={(e) => setBusqueda(e.target.value)}
          />

          {busqueda && (
            <button
              type="button"
              className="productos-limpiar"
              onClick={() => setBusqueda("")}
              title="Limpiar búsqueda"
            >
              ×
            </button>
          )}
        </div>

        <div className="productos-resultados">
          Mostrando <strong>{productosFiltrados.length}</strong> de{" "}
          <strong>{productos.length}</strong> productos
        </div>
      </div>

      {/* =====================================
          CONTENIDO
      ====================================== */}

      <div className="productos-layout">
        {/* =================================
            TABLA
        ================================== */}

        <div className="tarjeta-productos">
          <div className="tarjeta-grafica-header">
            <div>
              <h3>Productos disponibles</h3>

              <p>Selecciona un producto para ver su información detallada.</p>
            </div>
          </div>

          <div className="productos-tabla-scroll">
            <table className="tabla productos-tabla">
              <thead>
                <tr>
                  <th>Referencia</th>
                  <th>Descripción</th>
                  <th>Grupo</th>
                  <th>Planta</th>
                </tr>
              </thead>

              <tbody>
                {productosFiltrados.length === 0 ? (
                  <tr>
                    <td colSpan="4" className="productos-sin-resultados">
                      No se encontraron productos.
                    </td>
                  </tr>
                ) : (
                  productosFiltrados.map((producto) => (
                    <tr
                      key={producto.referencia}
                      className={
                        productoSeleccionado?.referencia === producto.referencia
                          ? "producto-seleccionado"
                          : ""
                      }
                      onClick={() => setProductoSeleccionado(producto)}
                    >
                      <td className="producto-referencia">
                        {producto.referencia}
                      </td>

                      <td>{producto.descripcion}</td>

                      <td>{producto.grupo || "-"}</td>

                      <td>{producto.planta || "-"}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>

        {/* =================================
            DETALLE DEL PRODUCTO
        ================================== */}

        <div className="producto-detalle">
          {productoSeleccionado ? (
            <>
              <div className="producto-detalle-header">
                <span className="producto-detalle-icono">📦</span>

                <div>
                  <span className="producto-detalle-referencia">
                    {productoSeleccionado.referencia}
                  </span>

                  <h3>{productoSeleccionado.descripcion}</h3>
                </div>
              </div>

              <div className="producto-detalle-info">
                <div className="detalle-item">
                  <span>Marca</span>

                  <strong>{productoSeleccionado.marca || "-"}</strong>
                </div>

                <div className="detalle-item">
                  <span>Línea</span>

                  <strong>{productoSeleccionado.linea || "-"}</strong>
                </div>

                <div className="detalle-item">
                  <span>Grupo</span>

                  <strong>{productoSeleccionado.grupo || "-"}</strong>
                </div>

                <div className="detalle-item">
                  <span>Clase</span>

                  <strong>{productoSeleccionado.clase || "-"}</strong>
                </div>

                <div className="detalle-item">
                  <span>Planta</span>

                  <strong>{productoSeleccionado.planta || "-"}</strong>
                </div>
              </div>
            </>
          ) : (
            <div className="producto-detalle-vacio">
              <div className="producto-detalle-vacio-icono">📦</div>

              <h3>Selecciona un producto</h3>

              <p>
                Haz clic sobre un producto de la tabla para consultar su
                información.
              </p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

export default Productos;
