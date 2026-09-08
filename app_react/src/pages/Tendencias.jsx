import { useEffect, useMemo, useState } from "react";
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Legend,
} from "recharts";

import { obtenerDashboard, obtenerHistorico } from "../services/api";
import "../styles/Tendencias.css";
import "../styles/SelectorProducto.css";

function Tendencias({ mesSeleccionado }) {
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
  // OBTENER DASHBOARD
  // =========================================

  useEffect(() => {
    const cargarDashboard = async () => {
      try {
        setCargando(true);
        setError("");

        const datos = await obtenerDashboard();

        setDashboard(datos);

        const meses = datos?.meses || [];

        if (meses.length > 0 && meses[0].productos?.length > 0) {
          setReferenciaSeleccionada(meses[0].productos[0].referencia);
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
    if (!dashboard?.meses) {
      return [];
    }

    const primerMes = dashboard.meses[0];

    return primerMes?.productos || [];
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
  // PRONÓSTICOS DEL PRODUCTO
  // =========================================

  const pronosticosProducto = useMemo(() => {
    if (!dashboard?.meses || !referenciaSeleccionada) {
      return [];
    }

    return dashboard.meses
      .filter((mes) => {
        if (!mesSeleccionado) {
          return true;
        }

        return mes.mes <= mesSeleccionado;
      })
      .map((mes) => {
        const producto = mes.productos?.find(
          (item) => item.referencia === referenciaSeleccionada,
        );

        if (!producto) {
          return null;
        }

        return {
          mes: mes.mes,
          cantidad: Number(producto.pronostico || 0),
        };
      })
      .filter(Boolean);
  }, [dashboard, referenciaSeleccionada, mesSeleccionado]);

  // =========================================
  // ÚLTIMO MES REAL
  // =========================================

  const ultimoMesReal = useMemo(() => {
    if (!historico.length) {
      return null;
    }

    return historico[historico.length - 1]?.mes || null;
  }, [historico]);

  // =========================================
  // DATOS PARA LA GRÁFICA
  // =========================================

  const datosGrafica = useMemo(() => {
    if (!historico.length) {
      return [];
    }

    const datos = [];

    // -----------------------------------------
    // HISTÓRICO REAL
    // -----------------------------------------

    historico.forEach((item) => {
      if (mesSeleccionado && item.mes > mesSeleccionado) {
        return;
      }

      datos.push({
        mes: item.mes,
        real: Number(item.cantidad || 0),
        prediccion: null,
      });
    });

    // -----------------------------------------
    // PREDICCIONES
    // -----------------------------------------

    const predicciones = pronosticosProducto.filter(
      (item) => item.mes > ultimoMesReal,
    );

    predicciones.forEach((item) => {
      datos.push({
        mes: item.mes,
        real: null,
        prediccion: item.cantidad,
      });
    });

    // Orden cronológico
    datos.sort((a, b) => a.mes.localeCompare(b.mes));

    // -----------------------------------------
    // CONECTAR ÚLTIMO REAL CON PREDICCIÓN
    // -----------------------------------------

    if (predicciones.length > 0 && historico.length > 0) {
      const ultimoReal = historico[historico.length - 1];

      const indiceUltimoReal = datos.findIndex(
        (item) => item.mes === ultimoReal.mes,
      );

      if (indiceUltimoReal >= 0) {
        datos[indiceUltimoReal] = {
          ...datos[indiceUltimoReal],
          prediccion: Number(ultimoReal.cantidad || 0),
        };
      }
    }

    return datos;
  }, [historico, pronosticosProducto, ultimoMesReal, mesSeleccionado]);

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
          <div className="info-producto">
            <span className="info-label">Referencia</span>

            <strong>{productoSeleccionado.referencia}</strong>
          </div>

          <div className="info-producto">
            <span className="info-label">Meses analizados</span>

            <strong>{historico.length}</strong>
          </div>

          <div className="info-producto">
            <span className="info-label">Total vendido</span>

            <strong>{totalHistorico.toLocaleString("es-CO")}</strong>
          </div>

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

            <p>Ventas reales y proyección de demanda</p>
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
                data={datosGrafica}
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
                  formatter={(valor, nombre) => [
                    Number(valor).toLocaleString("es-CO"),
                    nombre === "real" ? "Venta real" : "Predicción",
                  ]}
                  labelFormatter={(valor) => `Mes: ${valor}`}
                />

                <Legend />

                {/* =================================
                    VENTAS REALES
                ================================== */}

                <Line
                  type="monotone"
                  dataKey="real"
                  name="Venta real"
                  stroke="#b54a32"
                  strokeWidth={3}
                  dot={false}
                  activeDot={{
                    r: 6,
                  }}
                />

                {/* =================================
                    PREDICCIÓN
                ================================== */}

                <Line
                  type="monotone"
                  dataKey="prediccion"
                  name="Predicción"
                  stroke="#2563eb"
                  strokeWidth={3}
                  strokeDasharray="8 6"
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
              {historico
                .filter(
                  (item) => !mesSeleccionado || item.mes <= mesSeleccionado,
                )
                .map((item) => (
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
