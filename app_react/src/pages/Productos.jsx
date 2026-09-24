import { useEffect, useMemo, useState } from "react";
import { obtenerProductos } from "../services/api";
import EstadoCargando from "../components/EstadoCargando";
import MensajeError from "../components/MensajeError";
import "../styles/Productos.css";

const TAMANOS_PAGINA = [20, 50, 100];

function Productos() {
  const [productos, setProductos] = useState([]);
  const [busqueda, setBusqueda] = useState("");
  const [productoSeleccionado, setProductoSeleccionado] = useState(null);

  const [pagina, setPagina] = useState(1);
  const [tamanoPagina, setTamanoPagina] = useState(TAMANOS_PAGINA[0]);

  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState("");

  // El backend cachea el catálogo en memoria; solo la primera carga (o la
  // primera después de una actualización) lee el Excel y tarda más.
  // Si la respuesta no llega rápido, avisamos que puede estar importando.
  const [importandoPrimeraVez, setImportandoPrimeraVez] = useState(false);

  // =========================================
  // CARGAR PRODUCTOS
  // =========================================

  useEffect(() => {
    // Si la carga tarda, probablemente el backend está importando el
    // catálogo desde Excel por no tener aún nada en caché.
    const temporizador = setTimeout(() => {
      setImportandoPrimeraVez(true);
    }, 1200);

    const cargarProductos = async () => {
      try {
        setCargando(true);
        setError("");

        const datos = await obtenerProductos();

        const productosCargados = datos.productos || [];

        setProductos(productosCargados);

        if (productosCargados.length > 0) {
          setProductoSeleccionado(productosCargados[0]);
        }
      } catch (err) {
        console.error(err);

        setError("No fue posible obtener el catálogo de productos.");
      } finally {
        setCargando(false);
        clearTimeout(temporizador);
      }
    };

    cargarProductos();

    return () => clearTimeout(temporizador);
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
  // PAGINACIÓN
  // =========================================
  //
  // Con el catálogo completo mostrado como una sola tabla larga, cada
  // búsqueda o simple exploración obligaba a desplazarse por cientos de
  // filas. Se pagina el resultado ya filtrado, con un tamaño de página
  // elegible por el usuario.

  const totalPaginas = Math.max(
    1,
    Math.ceil(productosFiltrados.length / tamanoPagina),
  );

  // Si la búsqueda o el tamaño de página cambian, la página actual puede
  // quedar fuera de rango (por ejemplo, estar en la página 5 y que el nuevo
  // filtro solo tenga 2 páginas); se vuelve a la primera para no mostrar
  // una tabla vacía sin explicación.
  useEffect(() => {
    setPagina(1);
  }, [busqueda, tamanoPagina]);

  const productosPagina = useMemo(() => {
    const inicio = (pagina - 1) * tamanoPagina;

    return productosFiltrados.slice(inicio, inicio + tamanoPagina);
  }, [productosFiltrados, pagina, tamanoPagina]);

  const irAPaginaAnterior = () => {
    setPagina((actual) => Math.max(1, actual - 1));
  };

  const irAPaginaSiguiente = () => {
    setPagina((actual) => Math.min(totalPaginas, actual + 1));
  };

  // =========================================
  // ESTADO DE CARGA
  // =========================================

  if (cargando) {
    return (
      <EstadoCargando
        mensaje={
          importandoPrimeraVez
            ? "Importando el catálogo desde Excel por primera vez, esto puede tardar unos segundos..."
            : "Cargando catálogo de productos..."
        }
        claseAdicional="productos"
      />
    );
  }

  // =========================================
  // ERROR
  // =========================================

  if (error) {
    return <MensajeError mensaje={error} claseAdicional="productos" />;
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
                  productosPagina.map((producto) => (
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

          {productosFiltrados.length > 0 && (
            <div className="productos-paginacion">
              <div className="productos-tamano-pagina">
                <label htmlFor="tamano-pagina">Mostrar</label>

                <select
                  id="tamano-pagina"
                  value={tamanoPagina}
                  onChange={(e) => setTamanoPagina(Number(e.target.value))}
                >
                  {TAMANOS_PAGINA.map((tamano) => (
                    <option key={tamano} value={tamano}>
                      {tamano}
                    </option>
                  ))}
                </select>

                <span>productos por página</span>
              </div>

              <div className="productos-paginacion-controles">
                <button
                  type="button"
                  onClick={irAPaginaAnterior}
                  disabled={pagina === 1}
                >
                  ‹ Anterior
                </button>

                <span className="productos-paginacion-indicador">
                  Página {pagina} de {totalPaginas}
                </span>

                <button
                  type="button"
                  onClick={irAPaginaSiguiente}
                  disabled={pagina === totalPaginas}
                >
                  Siguiente ›
                </button>
              </div>
            </div>
          )}
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
