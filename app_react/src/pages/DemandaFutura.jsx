import { useEffect, useMemo, useState } from "react";
import { obtenerDashboard } from "../services/api";
import "../styles/DemandaFutura.css";
import "../styles/SelectorProducto.css";

function DemandaFutura({ mesSeleccionado }) {
  const [dashboard, setDashboard] = useState(null);
  const [referenciaSeleccionada, setReferenciaSeleccionada] = useState("");
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState("");

  // Búsqueda de productos
  const [mostrarBusqueda, setMostrarBusqueda] = useState(false);
  const [busquedaProducto, setBusquedaProducto] = useState("");

  // =========================================
  // CARGAR DASHBOARD
  // =========================================

  useEffect(() => {
    const cargarDatos = async () => {
      try {
        setCargando(true);
        setError("");

        const datos = await obtenerDashboard();

        setDashboard(datos);
      } catch (err) {
        console.error(err);

        setError("No fue posible cargar las estimaciones de demanda.");
      } finally {
        setCargando(false);
      }
    };

    cargarDatos();
  }, []);

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

  // =========================================
  // PRODUCTOS FILTRADOS POR BÚSQUEDA
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
  // ASEGURAR PRODUCTO SELECCIONADO
  // =========================================

  useEffect(() => {
    if (productos.length === 0) {
      setReferenciaSeleccionada("");
      return;
    }

    const productoExiste = productos.some(
      (producto) => producto.referencia === referenciaSeleccionada,
    );

    if (!productoExiste) {
      setReferenciaSeleccionada(productos[0].referencia);
    }
  }, [productos, referenciaSeleccionada]);

  // =========================================
  // PRODUCTO SELECCIONADO
  // =========================================

  const productoSeleccionado = useMemo(() => {
    return productos.find(
      (producto) => producto.referencia === referenciaSeleccionada,
    );
  }, [productos, referenciaSeleccionada]);

  // =========================================
  // FORMATEAR NÚMEROS
  // =========================================

  const formatearNumero = (numero) => {
    if (numero == null) {
      return "-";
    }

    return Math.round(numero).toLocaleString("es-CO");
  };

  // =========================================
  // MES DEL PRONÓSTICO
  // =========================================

  const mesPronostico = useMemo(() => {
    if (!mesSeleccionado) {
      return "-";
    }

    const fecha = new Date(`${mesSeleccionado}T00:00:00`);

    return fecha.toLocaleDateString("es-CO", {
      month: "long",
      year: "numeric",
    });
  }, [mesSeleccionado]);

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
    return (
      <div className="pagina">
        <div className="estado-cargando">
          Cargando estimaciones de demanda...
        </div>
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

            {/* BOTÓN DE BÚSQUEDA */}

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
                  <span className="detalle-etiqueta">REFERENCIA</span>

                  <h3>{productoSeleccionado.referencia}</h3>

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
