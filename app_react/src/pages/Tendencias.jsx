import { useEffect, useMemo, useState } from "react";
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from "recharts";

import { obtenerDashboard, obtenerHistorico } from "../services/api";

function Tendencias() {
  const [dashboard, setDashboard] = useState(null);
  const [referenciaSeleccionada, setReferenciaSeleccionada] = useState("");
  const [historico, setHistorico] = useState([]);

  const [cargando, setCargando] = useState(true);
  const [cargandoHistorico, setCargandoHistorico] = useState(false);

  const [error, setError] = useState("");
  const [errorHistorico, setErrorHistorico] = useState("");

  // =========================================
  // BÚSQUEDA DE PRODUCTOS
  // =========================================

  const [mostrarBusqueda, setMostrarBusqueda] = useState(false);

  const [busquedaProducto, setBusquedaProducto] = useState("");

  // =========================================
  // OBTENER CATÁLOGO DE PRODUCTOS
  // =========================================

  useEffect(() => {
    const cargarDashboard = async () => {
      try {
        setCargando(true);
        setError("");

        const datos = await obtenerDashboard();

        setDashboard(datos);

        // Seleccionamos inicialmente el primer producto
        if (datos?.productos?.length > 0) {
          setReferenciaSeleccionada(datos.productos[0].referencia);
        }
      } catch (err) {
        console.error(err);

        setError("No fue posible obtener la información de ventas.");
      } finally {
        setCargando(false);
      }
    };

    cargarDashboard();
  }, []);

  // =========================================
  // OBTENER HISTÓRICO
  // =========================================

  useEffect(() => {
    if (!referenciaSeleccionada) {
      return;
    }

    const cargarHistorico = async () => {
      try {
        setCargandoHistorico(true);
        setErrorHistorico("");

        const datos = await obtenerHistorico(referenciaSeleccionada);

        setHistorico(datos.historico || []);
      } catch (err) {
        console.error(err);

        setErrorHistorico("No fue posible obtener el histórico del producto.");
      } finally {
        setCargandoHistorico(false);
      }
    };

    cargarHistorico();
  }, [referenciaSeleccionada]);

  // =========================================
  // PRODUCTOS
  // =========================================

  const productos = useMemo(() => {
    return dashboard?.productos || [];
  }, [dashboard]);

  // =========================================
  // PRODUCTOS FILTRADOS
  // =========================================

  const productosFiltrados = useMemo(() => {
    const texto = busquedaProducto.trim().toLowerCase();

    if (!texto) {
      return productos;
    }

    return productos.filter((producto) =>
      producto.referencia.toString().toLowerCase().includes(texto),
    );
  }, [productos, busquedaProducto]);

  // =========================================
  // PRODUCTO SELECCIONADO
  // =========================================

  const productoSeleccionado = productos.find(
    (producto) => producto.referencia === referenciaSeleccionada,
  );

  // =========================================
  // TOTALES
  // =========================================

  const totalHistorico = historico.reduce(
    (total, item) => total + Number(item.cantidad || 0),
    0,
  );

  const promedioMensual =
    historico.length > 0 ? totalHistorico / historico.length : 0;

  // =========================================
  // CARGANDO
  // =========================================

  if (cargando) {
    return (
      <div className="pagina">
        <div className="estado-cargando">Cargando tendencias de ventas...</div>
      </div>
    );
  }

  // =========================================
  // ERROR
  // =========================================

  if (error) {
    return (
      <div className="pagina">
        <div className="mensaje-error">{error}</div>
      </div>
    );
  }

  return (
    <div className="pagina tendencias">
      {/* =====================================
          ENCABEZADO
      ====================================== */}

      <div className="pagina-encabezado">
        <div>
          <h2>Tendencias de compra</h2>

          <p>
            Analiza el comportamiento histórico de las ventas y detecta patrones
            de demanda.
          </p>
        </div>

        {/* =================================
            SELECTOR DE PRODUCTO
        ================================== */}

        <div className="selector-producto">
          <label>Producto</label>

          <div className="selector-producto-control">
            {/* DESPLEGABLE */}

            <select
              value={referenciaSeleccionada}
              onChange={(e) => setReferenciaSeleccionada(e.target.value)}
            >
              {productosFiltrados.map((producto) => (
                <option key={producto.referencia} value={producto.referencia}>
                  {producto.referencia}
                </option>
              ))}
            </select>

            {/* BOTÓN LUPA */}

            <button
              type="button"
              className={`boton-busqueda ${mostrarBusqueda ? "activo" : ""}`}
              onClick={() => {
                setMostrarBusqueda(!mostrarBusqueda);

                if (mostrarBusqueda) {
                  setBusquedaProducto("");
                }
              }}
              title="Buscar referencia"
            >
              🔍
            </button>
          </div>

          {/* =================================
              CAMPO DE BÚSQUEDA
          ================================== */}

          {mostrarBusqueda && (
            <div className="campo-busqueda-producto">
              <input
                type="text"
                placeholder="Buscar referencia..."
                value={busquedaProducto}
                onChange={(e) => setBusquedaProducto(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === "Enter") {
                    if (productosFiltrados.length > 0) {
                      setReferenciaSeleccionada(
                        productosFiltrados[0].referencia,
                      );

                      setBusquedaProducto("");
                      setMostrarBusqueda(false);
                    }
                  }
                }}
                autoFocus
              />

              {busquedaProducto && (
                <span className="resultado-busqueda">
                  {productosFiltrados.length} resultado
                  {productosFiltrados.length !== 1 ? "s" : ""}
                </span>
              )}
            </div>
          )}
        </div>
      </div>

      {/* =====================================
          INFORMACIÓN DEL PRODUCTO
      ====================================== */}

      {productoSeleccionado && (
        <div className="tendencias-info">
          {/* REFERENCIA */}

          <div className="info-producto">
            <span className="info-label">Referencia</span>

            <strong>{productoSeleccionado.referencia}</strong>
          </div>

          {/* MESES */}

          <div className="info-producto">
            <span className="info-label">Meses analizados</span>

            <strong>{historico.length}</strong>
          </div>

          {/* TOTAL */}

          <div className="info-producto">
            <span className="info-label">Total vendido</span>

            <strong>{totalHistorico.toLocaleString("es-CO")}</strong>
          </div>

          {/* PROMEDIO */}

          <div className="info-producto">
            <span className="info-label">Promedio mensual</span>

            <strong>
              {Math.round(promedioMensual).toLocaleString("es-CO")}
            </strong>
          </div>
        </div>
      )}

      {/* =====================================
          GRÁFICA
      ====================================== */}

      <div className="tarjeta-grafica">
        <div className="tarjeta-grafica-header">
          <div>
            <h3>Evolución histórica</h3>

            <p>Cantidad vendida por mes</p>
          </div>
        </div>

        {cargandoHistorico ? (
          <div className="estado-cargando-grafica">Cargando histórico...</div>
        ) : errorHistorico ? (
          <div className="mensaje-error">{errorHistorico}</div>
        ) : (
          <div className="grafica-container">
            <ResponsiveContainer width="100%" height={420}>
              <LineChart
                data={historico}
                margin={{
                  top: 10,
                  right: 20,
                  left: 10,
                  bottom: 10,
                }}
              >
                <CartesianGrid strokeDasharray="3 3" />

                <XAxis
                  dataKey="mes"
                  tickFormatter={(valor) => valor.substring(0, 7)}
                />

                <YAxis />

                <Tooltip
                  formatter={(valor) => Number(valor).toLocaleString("es-CO")}
                  labelFormatter={(valor) => `Mes: ${valor}`}
                />

                <Line
                  type="monotone"
                  dataKey="cantidad"
                  stroke="#b54a32"
                  strokeWidth={3}
                  dot={false}
                  activeDot={{
                    r: 6,
                  }}
                />
              </LineChart>
            </ResponsiveContainer>
          </div>
        )}
      </div>

      {/* =====================================
          TABLA HISTÓRICA
      ====================================== */}

      <div className="tarjeta-historico">
        <div className="tarjeta-grafica-header">
          <div>
            <h3>Histórico mensual</h3>

            <p>Detalle de las cantidades vendidas.</p>
          </div>
        </div>

        <div className="tabla-scroll">
          <table className="tabla">
            <thead>
              <tr>
                <th>Mes</th>
                <th>Cantidad</th>
              </tr>
            </thead>

            <tbody>
              {historico.map((item) => (
                <tr key={item.mes}>
                  <td>{item.mes}</td>

                  <td>{Number(item.cantidad).toLocaleString("es-CO")}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

export default Tendencias;
