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

import { obtenerHistorico } from "../services/api";
import { useDashboard } from "../hooks/useDashboard";
import { useCatalogoProductos } from "../hooks/useCatalogoProductos";
import SelectorProducto from "../components/SelectorProducto";
import TarjetaPeriodo from "../components/TarjetaPeriodo";
import EstadoCargando from "../components/EstadoCargando";
import MensajeError from "../components/MensajeError";
import "../styles/Tendencias.css";

function Tendencias({ mesSeleccionado, mesesDisponibles, setMesSeleccionado }) {
  const {
    dashboard,
    cargando,
    error,
  } = useDashboard("No fue posible obtener la información de ventas.");

  const { obtenerNombreProducto } = useCatalogoProductos();

  const [referenciaSeleccionada, setReferenciaSeleccionada] = useState("");
  const [historico, setHistorico] = useState([]);

  const [cargandoHistorico, setCargandoHistorico] = useState(false);
  const [errorHistorico, setErrorHistorico] = useState("");

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

  // Si el usuario aún no ha elegido nada, se usa el producto con mayor
  // demanda proyectada como selección por defecto.
  const referenciaMayorDemanda = useMemo(() => {
    if (productos.length === 0) {
      return "";
    }

    return productos.reduce((mayor, actual) =>
      Number(actual.pronostico || 0) > Number(mayor.pronostico || 0)
        ? actual
        : mayor,
    ).referencia;
  }, [productos]);

  const referenciaEfectiva = referenciaSeleccionada || referenciaMayorDemanda;

  // =========================================
  // OBTENER HISTÓRICO
  // =========================================

  useEffect(() => {
    if (!referenciaEfectiva) {
      return;
    }

    const cargarHistorico = async () => {
      try {
        setCargandoHistorico(true);
        setErrorHistorico("");

        const datos = await obtenerHistorico(referenciaEfectiva);

        setHistorico(datos.historico || []);
      } catch (err) {
        console.error(err);

        setErrorHistorico("No fue posible obtener el histórico del producto.");
      } finally {
        setCargandoHistorico(false);
      }
    };

    cargarHistorico();
  }, [referenciaEfectiva]);

  // =========================================
  // PRODUCTO SELECCIONADO
  // =========================================

  const productoSeleccionado = productos.find(
    (producto) => producto.referencia === referenciaEfectiva,
  );

  // =========================================
  // PRONÓSTICOS DEL PRODUCTO
  // =========================================

  const pronosticosProducto = useMemo(() => {
    if (!dashboard?.meses || !referenciaEfectiva) {
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
          (item) => item.referencia === referenciaEfectiva,
        );

        if (!producto) {
          return null;
        }

        return {
          mes: mes.mes,
          cantidad: Number(producto.pronostico || 0),
          inferior: producto.inferior != null ? Number(producto.inferior) : null,
          superior: producto.superior != null ? Number(producto.superior) : null,
        };
      })
      .filter(Boolean);
  }, [dashboard, referenciaEfectiva, mesSeleccionado]);

  // =========================================
  // DATOS PARA LA GRÁFICA
  // =========================================

  // Combina, mes a mes, la venta real con el mínimo/máximo que el modelo
  // había estimado para ese mismo mes: para meses ya pasados es el backtest
  // causal (qué habría predicho con la información disponible hasta
  // entonces) y para meses futuros es el pronóstico vigente.
  const datosGrafica = useMemo(() => {
    if (!historico.length) {
      return [];
    }

    const filasPorMes = new Map();

    historico.forEach((item) => {
      if (mesSeleccionado && item.mes > mesSeleccionado) {
        return;
      }

      filasPorMes.set(item.mes, {
        mes: item.mes,
        real: Number(item.cantidad || 0),
        minimo: null,
        maximo: null,
      });
    });

    pronosticosProducto.forEach((item) => {
      if (item.inferior == null || item.superior == null) {
        return;
      }

      const filaExistente = filasPorMes.get(item.mes);

      filasPorMes.set(item.mes, {
        mes: item.mes,
        real: filaExistente?.real ?? null,
        minimo: item.inferior,
        maximo: item.superior,
      });
    });

    return Array.from(filasPorMes.values()).sort((a, b) =>
      a.mes.localeCompare(b.mes),
    );
  }, [historico, pronosticosProducto, mesSeleccionado]);

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
    return <EstadoCargando mensaje="Cargando tendencias de ventas..." />;
  }

  // =========================================
  // ERROR
  // =========================================

  if (error) {
    return <MensajeError mensaje={error} />;
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

        <TarjetaPeriodo
          mes={mesSeleccionado}
          etiqueta="Horizonte mostrado"
          descripcion="Hasta este mes se muestran predicciones"
          mesesDisponibles={mesesDisponibles}
          onCambiarMes={setMesSeleccionado}
        />
      </div>

      {/* =====================================
          INFORMACIÓN DEL PRODUCTO
      ====================================== */}

      {productoSeleccionado && (
        <div className="tendencias-info">
          <div className="info-producto">
            <span className="info-label">Producto seleccionado</span>

            <strong>
              {obtenerNombreProducto(productoSeleccionado.referencia)}
            </strong>

            <span className="info-codigo">
              {productoSeleccionado.referencia}
            </span>
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
          SELECTOR DE PRODUCTO
      ====================================== */}

      <div className="selector-producto-seccion">
        <SelectorProducto
          productos={productos}
          valorSeleccionado={referenciaEfectiva}
          onSeleccionar={setReferenciaSeleccionada}
          obtenerEtiquetaProducto={(producto) =>
            obtenerNombreProducto(producto.referencia)
          }
        />
      </div>

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
                    nombre,
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
                    MÍNIMO ESTIMADO
                ================================== */}

                <Line
                  type="monotone"
                  dataKey="minimo"
                  name="Mínimo estimado"
                  stroke="#c78228"
                  strokeWidth={2}
                  strokeDasharray="6 4"
                  dot={false}
                  activeDot={{
                    r: 5,
                  }}
                />

                {/* =================================
                    MÁXIMO ESTIMADO
                ================================== */}

                <Line
                  type="monotone"
                  dataKey="maximo"
                  name="Máximo estimado"
                  stroke="#66815d"
                  strokeWidth={2}
                  strokeDasharray="6 4"
                  dot={false}
                  activeDot={{
                    r: 5,
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
