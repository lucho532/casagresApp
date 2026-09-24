import { useMemo, useState } from "react";
import { useDashboard } from "../hooks/useDashboard";
import { useCatalogoProductos } from "../hooks/useCatalogoProductos";
import { formatearNumero, formatearMes } from "../utils/formato";
import SelectorProducto from "../components/SelectorProducto";
import TarjetaPeriodo from "../components/TarjetaPeriodo";
import EstadoCargando from "../components/EstadoCargando";
import MensajeError from "../components/MensajeError";
import "../styles/DemandaFutura.css";

function DemandaFutura({ mesSeleccionado, mesesDisponibles, setMesSeleccionado }) {
  const {
    dashboard,
    cargando,
    error,
  } = useDashboard("No fue posible cargar las estimaciones de demanda.");

  const { obtenerNombreProducto } = useCatalogoProductos();

  const [referenciaSeleccionada, setReferenciaSeleccionada] = useState("");

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

    return [...dashboardMesSeleccionado.productos]
      .filter((producto) => producto.pronostico > 0)
      .sort((a, b) => b.pronostico - a.pronostico);
  }, [dashboardMesSeleccionado]);

  // Si la selección actual no existe en el mes vigente (o aún no hay
  // ninguna), se usa el primer producto disponible.
  const referenciaEfectiva = useMemo(() => {
    const existe = productos.some(
      (producto) => producto.referencia === referenciaSeleccionada,
    );

    return existe ? referenciaSeleccionada : productos[0]?.referencia || "";
  }, [productos, referenciaSeleccionada]);

  // =========================================
  // PRODUCTO SELECCIONADO
  // =========================================

  const productoSeleccionado = useMemo(() => {
    return productos.find(
      (producto) => producto.referencia === referenciaEfectiva,
    );
  }, [productos, referenciaEfectiva]);

  // =========================================
  // MES DEL PRONÓSTICO
  // =========================================

  const mesPronostico = useMemo(
    () => formatearMes(mesSeleccionado),
    [mesSeleccionado],
  );

  // =========================================
  // NIVEL DE CONFIANZA
  // =========================================

  const confianza = useMemo(() => {
    if (!productoSeleccionado || productoSeleccionado.alphaAci == null) {
      return null;
    }

    return Math.round((1 - productoSeleccionado.alphaAci) * 100);
  }, [productoSeleccionado]);

  // =========================================
  // CARGANDO
  // =========================================

  if (cargando) {
    return <EstadoCargando mensaje="Cargando estimaciones de demanda..." />;
  }

  // =========================================
  // ERROR
  // =========================================

  if (error) {
    return <MensajeError mensaje={error} />;
  }

  // =========================================
  // INTERFAZ
  // =========================================

  return (
    <div className="pagina demanda-futura">
      {/* =====================================
          ENCABEZADO
      ====================================== */}

      <div className="pagina-encabezado">
        <div>
          <h2>Demanda futura</h2>

          <p>Estimación de demanda para los próximos periodos.</p>
        </div>

        <TarjetaPeriodo
          mes={mesSeleccionado}
          etiqueta="Periodo proyectado"
          descripcion="Mes al que corresponde la demanda mostrada"
          mesesDisponibles={mesesDisponibles}
          onCambiarMes={setMesSeleccionado}
        />
      </div>

      {/* =====================================
          INFORMACIÓN DEL PRODUCTO
      ====================================== */}

      {productoSeleccionado && (
        <>
          {/* =================================
              TARJETAS DE RESUMEN
          ================================== */}

          <div className="demanda-resumen">
            {/* DEMANDA ESTIMADA */}

            <div className="demanda-card">
              <div className="demanda-icono">🔮</div>

              <div>
                <span>Demanda estimada</span>

                <strong>
                  {formatearNumero(productoSeleccionado.pronostico)}
                </strong>

                <small>{mesPronostico}</small>
              </div>
            </div>

            {/* ESCENARIO MÍNIMO */}

            <div className="demanda-card">
              <div className="demanda-icono minimo">↓</div>

              <div>
                <span>Escenario mínimo</span>

                <strong>
                  {formatearNumero(productoSeleccionado.inferior)}
                </strong>

                <small>Límite inferior</small>
              </div>
            </div>

            {/* ESCENARIO MÁXIMO */}

            <div className="demanda-card">
              <div className="demanda-icono maximo">↑</div>

              <div>
                <span>Escenario máximo</span>

                <strong>
                  {formatearNumero(productoSeleccionado.superior)}
                </strong>

                <small>Límite superior</small>
              </div>
            </div>

            {/* CONFIANZA */}

            <div className="demanda-card">
              <div className="demanda-icono confianza">✓</div>

              <div>
                <span>Confianza</span>

                <strong>{confianza != null ? `${confianza}%` : "-"}</strong>

                <small>Nivel de confianza</small>
              </div>
            </div>
          </div>

          {/* =================================
              SELECTOR DE PRODUCTO
          ================================== */}

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

          {/* =================================
              DETALLE DE DEMANDA
          ================================== */}

          <div className="detalle-demanda">
            {/* =================================
                TARJETA PRINCIPAL
            ================================== */}

            <div className="tarjeta-demanda-principal">
              {/* CABECERA */}

              <div className="tarjeta-demanda-header">
                <div>
                  <span className="detalle-etiqueta">PRODUCTO</span>

                  <h3>{obtenerNombreProducto(productoSeleccionado.referencia)}</h3>

                  <span className="detalle-codigo">
                    {productoSeleccionado.referencia}
                  </span>

                  <p>Proyección para {mesPronostico}</p>
                </div>

                {/* MÉTODO */}

                <div
                  className={
                    productoSeleccionado.metodo === "KRR"
                      ? "metodo-badge metodo-krr"
                      : "metodo-badge metodo-anterior"
                  }
                >
                  {productoSeleccionado.metodo || "Sin método"}
                </div>
              </div>

              {/* =================================
                  PRONÓSTICO
              ================================== */}

              <div className="pronostico-visual">
                <div className="pronostico-valor">
                  <span>DEMANDA PROYECTADA</span>

                  <strong>
                    {formatearNumero(productoSeleccionado.pronostico)}
                  </strong>

                  <small>unidades estimadas</small>
                </div>

                {/* =================================
                    INTERVALO
                ================================== */}

                <div className="intervalo-visual">
                  <div className="intervalo-titulo">
                    Rango estimado de demanda
                  </div>

                  <div className="intervalo-barra">
                    <div className="intervalo-linea"></div>

                    <div className="intervalo-punto inferior-punto"></div>

                    <div className="intervalo-punto pronostico-punto"></div>

                    <div className="intervalo-punto superior-punto"></div>
                  </div>

                  <div className="intervalo-valores">
                    {/* MÍNIMO */}

                    <div>
                      <span>Mínimo</span>

                      <strong>
                        {formatearNumero(productoSeleccionado.inferior)}
                      </strong>
                    </div>

                    {/* ESTIMADO */}

                    <div>
                      <span>Estimado</span>

                      <strong>
                        {formatearNumero(productoSeleccionado.pronostico)}
                      </strong>
                    </div>

                    {/* MÁXIMO */}

                    <div>
                      <span>Máximo</span>

                      <strong>
                        {formatearNumero(productoSeleccionado.superior)}
                      </strong>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            {/* =================================
                INFORMACIÓN DEL MODELO
            ================================== */}

            <div className="modelo-info">
              <div className="modelo-info-header">
                <h3>Información del modelo</h3>

                <p>Información utilizada para generar la estimación.</p>
              </div>

              {/* MÉTODO */}

              <div className="modelo-item">
                <span>Método de predicción</span>

                <strong>{productoSeleccionado.metodo || "-"}</strong>
              </div>

              {/* PERIODO */}

              <div className="modelo-item">
                <span>Periodo proyectado</span>

                <strong>{mesPronostico}</strong>
              </div>

              {/* CONFIANZA */}

              <div className="modelo-item">
                <span>Nivel de confianza</span>

                <strong>{confianza != null ? `${confianza}%` : "-"}</strong>
              </div>

              {/* ESTADO */}

              <div className="modelo-item">
                <span>Estado</span>

                <strong className="estado-procesado">Procesado</strong>
              </div>
            </div>
          </div>
        </>
      )}

      {/* =====================================
          SIN PRODUCTOS
      ====================================== */}

      {productos.length === 0 && (
        <div className="sin-productos-demanda">
          <div className="sin-productos-icono">📦</div>

          <h3>No hay pronósticos disponibles</h3>

          <p>No existen productos con demanda futura proyectada.</p>
        </div>
      )}
    </div>
  );
}

export default DemandaFutura;
