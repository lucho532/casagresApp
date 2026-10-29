import { useMemo } from "react";
import { useDashboard } from "../hooks/useDashboard";
import { useCatalogoProductos } from "../hooks/useCatalogoProductos";
import { formatearNumero, formatearMes } from "../utils/formato";
import EstadoCargando from "../components/EstadoCargando";
import MensajeError from "../components/MensajeError";
import "../styles/Inicio.css";

function Inicio({
  cambiarPagina,
  mesSeleccionado,
  setMesSeleccionado,
  mesesDisponibles,
}) {
  const { dashboard, cargando, error } = useDashboard(
    "No fue posible cargar el resumen de la plataforma.",
  );

  const { obtenerNombreProducto, obtenerNombreConCodigo } =
    useCatalogoProductos();

  // =========================================
  // MES SELECCIONADO
  // =========================================

  const dashboardMesSeleccionado = useMemo(() => {
    if (!dashboard?.meses) {
      return null;
    }

    return dashboard.meses.find((mes) => mes.mes === mesSeleccionado);
  }, [dashboard, mesSeleccionado]);

  // =========================================
  // PRODUCTOS DEL MES SELECCIONADO
  // =========================================

  const productos = useMemo(() => {
    if (!dashboardMesSeleccionado?.productos) {
      return [];
    }

    return dashboardMesSeleccionado.productos.filter(
      (producto) => producto.pronostico > 0,
    );
  }, [dashboardMesSeleccionado]);

  // =========================================
  // PRODUCTOS CON MAYOR DEMANDA
  // =========================================

  const productosMayorDemanda = useMemo(() => {
    return [...productos]
      .sort((a, b) => b.pronostico - a.pronostico)
      .slice(0, 5);
  }, [productos]);

  // =========================================
  // DEMANDA TOTAL
  // =========================================

  const demandaTotal = useMemo(() => {
    return productos.reduce(
      (total, producto) => total + Number(producto.pronostico || 0),
      0,
    );
  }, [productos]);

  // =========================================
  // PRODUCTO PRINCIPAL
  // =========================================

  const productoPrincipal = productosMayorDemanda[0];

  // =========================================
  // CAMBIAR MES GLOBAL
  // =========================================

  const cambiarMes = (mes) => {
    setMesSeleccionado(mes);
  };

  // =========================================
  // CARGANDO
  // =========================================

  if (cargando) {
    return <EstadoCargando mensaje="Cargando resumen de la plataforma..." />;
  }

  // =========================================
  // ERROR
  // =========================================

  if (error) {
    return <MensajeError mensaje={error} />;
  }

  return (
    <div className="pagina inicio">
      {/* =====================================
          BIENVENIDA
      ====================================== */}

      <div className="inicio-bienvenida">
        <div>
          <span className="inicio-etiqueta">CASAGRES · ANALÍTICA</span>

          <h2>Resumen de la plataforma</h2>

          <p>
            Consulta el comportamiento de las ventas y las estimaciones de
            demanda para apoyar la toma de decisiones.
          </p>
        </div>

        {/* =================================
            SELECTOR GLOBAL DE PERIODO
        ================================== */}

        <div className="inicio-periodo">
          <span>Horizonte mostrado</span>

          <select
            value={mesSeleccionado}
            onChange={(e) => cambiarMes(e.target.value)}
          >
            {mesesDisponibles.map((mes) => (
              <option key={mes.mes} value={mes.mes}>
                {formatearMes(mes.mes)}
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* =====================================
          INDICADORES PRINCIPALES
      ====================================== */}

      <div className="inicio-kpis">
        {/* PRODUCTOS */}

        <div className="inicio-kpi">
          <div className="inicio-kpi-icono">📦</div>

          <div>
            <span>Productos analizados</span>

            <strong>{productos.length.toLocaleString("es-CO")}</strong>

            <small>Con demanda proyectada</small>
          </div>
        </div>

        {/* DEMANDA TOTAL */}

        <div className="inicio-kpi">
          <div className="inicio-kpi-icono demanda">🔮</div>

          <div>
            <span>Demanda proyectada</span>

            <strong>{formatearNumero(demandaTotal)}</strong>

            <small>Unidades estimadas</small>
          </div>
        </div>

        {/* PRODUCTO PRINCIPAL */}

        <div className="inicio-kpi">
          <div className="inicio-kpi-icono producto">🏆</div>

          <div>
            <span>Mayor demanda</span>

            <strong>
              {productoPrincipal
                ? formatearNumero(productoPrincipal.pronostico)
                : "-"}
            </strong>

            <small>
              {productoPrincipal
                ? obtenerNombreConCodigo(productoPrincipal.referencia)
                : "Sin datos"}
            </small>
          </div>
        </div>

        {/* PERIODO */}

        <div className="inicio-kpi">
          <div className="inicio-kpi-icono periodo">📅</div>

          <div>
            <span>Periodo proyectado</span>

            <strong className="inicio-kpi-periodo">
              {formatearMes(mesSeleccionado)}
            </strong>

            <small>Última estimación disponible</small>
          </div>
        </div>
      </div>

      {/* =====================================
          CONTENIDO PRINCIPAL
      ====================================== */}

      <div className="inicio-contenido">
        {/* =================================
            TOP PRODUCTOS
        ================================== */}

        <div className="inicio-ranking">
          <div className="inicio-seccion-header">
            <div>
              <h3>Productos con mayor demanda</h3>

              <p>
                Productos con mayor demanda proyectada para el próximo
                periodo.
              </p>
            </div>

            <span className="inicio-badge">Top 5</span>
          </div>

          <div className="inicio-ranking-lista">
            {productosMayorDemanda.map((producto, indice) => (
              <div className="inicio-ranking-item" key={producto.referencia}>
                <div className="ranking-posicion">{indice + 1}</div>

                <div className="ranking-producto">
                  <strong>{obtenerNombreProducto(producto.referencia)}</strong>

                  <span>
                    {producto.referencia} · {producto.metodo || "Sin método"}
                  </span>
                </div>

                <div className="ranking-demanda">
                  <strong>{formatearNumero(producto.pronostico)}</strong>

                  <span>unidades</span>
                </div>
              </div>
            ))}

            {productosMayorDemanda.length === 0 && (
              <div className="inicio-sin-datos">
                No hay productos disponibles.
              </div>
            )}
          </div>
        </div>

        {/* =================================
            ACCESOS RÁPIDOS
        ================================== */}

        <div className="inicio-accesos">
          <div className="inicio-seccion-header">
            <div>
              <h3>Explorar plataforma</h3>

              <p>Accede rápidamente a las principales herramientas.</p>
            </div>
          </div>

          <div className="inicio-accesos-lista">
            <div
              className="inicio-acceso"
              onClick={() => cambiarPagina("tendencias")}
              role="button"
              tabIndex={0}
              onKeyDown={(e) => {
                if (e.key === "Enter" || e.key === " ") {
                  cambiarPagina("tendencias");
                }
              }}
            >
              <div className="acceso-icono">↗</div>

              <div>
                <strong>Tendencias de compra</strong>

                <span>Analiza el histórico de ventas.</span>
              </div>
            </div>

            <div
              className="inicio-acceso"
              onClick={() => cambiarPagina("demanda")}
              role="button"
              tabIndex={0}
              onKeyDown={(e) => {
                if (e.key === "Enter" || e.key === " ") {
                  cambiarPagina("demanda");
                }
              }}
            >
              <div className="acceso-icono">🔮</div>

              <div>
                <strong>Demanda futura</strong>

                <span>Consulta las proyecciones.</span>
              </div>
            </div>

            <div
              className="inicio-acceso"
              onClick={() => cambiarPagina("decisiones")}
              role="button"
              tabIndex={0}
              onKeyDown={(e) => {
                if (e.key === "Enter" || e.key === " ") {
                  cambiarPagina("decisiones");
                }
              }}
            >
              <div className="acceso-icono">✓</div>

              <div>
                <strong>Enfoque en decisiones</strong>

                <span>Convierte los datos en acciones.</span>
              </div>
            </div>

            <div
              className="inicio-acceso"
              onClick={() => cambiarPagina("powerbi")}
              role="button"
              tabIndex={0}
              onKeyDown={(e) => {
                if (e.key === "Enter" || e.key === " ") {
                  cambiarPagina("powerbi");
                }
              }}
            >
              <div className="acceso-icono">▦</div>

              <div>
                <strong>Power BI</strong>

                <span>Explora los informes interactivos.</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default Inicio;
