import { useEffect, useMemo, useState } from "react";
import { obtenerDashboard } from "../services/api";

function Decisiones() {
  const [dashboard, setDashboard] = useState(null);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    const cargarDatos = async () => {
      try {
        setCargando(true);
        setError("");

        const datos = await obtenerDashboard();

        setDashboard(datos);
      } catch (err) {
        console.error(err);

        setError(
          "No fue posible cargar la información para la toma de decisiones.",
        );
      } finally {
        setCargando(false);
      }
    };

    cargarDatos();
  }, []);

  // =========================================
  // PRODUCTOS
  // =========================================

  const productos = useMemo(() => {
    return dashboard?.productos || [];
  }, [dashboard]);

  // =========================================
  // PRODUCTOS ORDENADOS POR DEMANDA
  // =========================================

  const productosOrdenados = useMemo(() => {
    return [...productos]
      .filter((producto) => Number(producto.pronostico || 0) > 0)
      .sort((a, b) => Number(b.pronostico || 0) - Number(a.pronostico || 0));
  }, [productos]);

  // =========================================
  // ESTADÍSTICAS
  // =========================================

  const estadisticas = useMemo(() => {
    const total = productosOrdenados.reduce(
      (suma, producto) => suma + Number(producto.pronostico || 0),
      0,
    );

    const alta = productosOrdenados.filter(
      (producto) => Number(producto.pronostico || 0) >= total / 10,
    ).length;

    const krr = productosOrdenados.filter(
      (producto) => producto.metodo === "KRR",
    ).length;

    const conIntervalo = productosOrdenados.filter(
      (producto) => producto.inferior != null && producto.superior != null,
    ).length;

    return {
      total,
      alta,
      krr,
      conIntervalo,
    };
  }, [productosOrdenados]);

  // =========================================
  // PRODUCTOS PRINCIPALES
  // =========================================

  const mayorDemanda = productosOrdenados.slice(0, 5);

  // =========================================
  // FORMATO
  // =========================================

  const formatearNumero = (numero) => {
    return Math.round(Number(numero || 0)).toLocaleString("es-CO");
  };

  // =========================================
  // CARGANDO
  // =========================================

  if (cargando) {
    return (
      <div className="pagina">
        <div className="estado-cargando">
          Analizando información para decisiones...
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

  return (
    <div className="pagina decisiones">
      {/* =====================================
          ENCABEZADO
      ====================================== */}

      <div className="pagina-encabezado decisiones-encabezado">
        <div>
          <span className="decisiones-etiqueta">APOYO A LA DECISIÓN</span>

          <h2>Enfoque en decisiones</h2>

          <p>
            Identifica las referencias que requieren mayor atención a partir de
            la demanda proyectada.
          </p>
        </div>

        <div className="decisiones-periodo">
          <span>Periodo analizado</span>

          <strong>{dashboard?.mes || "-"}</strong>
        </div>
      </div>

      {/* =====================================
          KPIs
      ====================================== */}

      <div className="decisiones-kpis">
        <div className="decision-kpi">
          <div className="decision-kpi-icono">🎯</div>

          <div>
            <span>Demanda proyectada</span>

            <strong>{formatearNumero(estadisticas.total)}</strong>

            <small>Unidades totales</small>
          </div>
        </div>

        <div className="decision-kpi">
          <div className="decision-kpi-icono alerta">⚠</div>

          <div>
            <span>Alta demanda</span>

            <strong>{estadisticas.alta}</strong>

            <small>Referencias prioritarias</small>
          </div>
        </div>

        <div className="decision-kpi">
          <div className="decision-kpi-icono modelo">◈</div>

          <div>
            <span>Modelo KRR</span>

            <strong>{estadisticas.krr}</strong>

            <small>Pronósticos con KRR</small>
          </div>
        </div>

        <div className="decision-kpi">
          <div className="decision-kpi-icono intervalo">↕</div>

          <div>
            <span>Con intervalo</span>

            <strong>{estadisticas.conIntervalo}</strong>

            <small>Pronósticos con rango</small>
          </div>
        </div>
      </div>

      {/* =====================================
          MENSAJE PRINCIPAL
      ====================================== */}

      <div className="decision-recomendacion">
        <div className="decision-recomendacion-icono">💡</div>

        <div>
          <span>PRINCIPAL FOCO DE ATENCIÓN</span>

          {mayorDemanda.length > 0 ? (
            <h3>{mayorDemanda[0].referencia}</h3>
          ) : (
            <h3>No hay información disponible</h3>
          )}

          <p>
            Esta referencia presenta la mayor demanda proyectada entre los
            productos analizados.
          </p>
        </div>

        {mayorDemanda.length > 0 && (
          <div className="decision-recomendacion-valor">
            <strong>{formatearNumero(mayorDemanda[0].pronostico)}</strong>

            <span>unidades proyectadas</span>
          </div>
        )}
      </div>

      {/* =====================================
          RANKING
      ====================================== */}

      <div className="decision-tabla">
        <div className="decision-tabla-header">
          <div>
            <h3>Referencias prioritarias</h3>

            <p>Productos ordenados según su demanda proyectada.</p>
          </div>

          <span className="decision-badge">
            {productosOrdenados.length} productos
          </span>
        </div>

        <div className="tabla-scroll">
          <table className="tabla decisiones-tabla">
            <thead>
              <tr>
                <th>Posición</th>

                <th>Referencia</th>

                <th>Demanda proyectada</th>

                <th>Intervalo</th>

                <th>Método</th>

                <th>Prioridad</th>
              </tr>
            </thead>

            <tbody>
              {productosOrdenados.map((producto, indice) => {
                const posicion = indice + 1;

                let prioridad = "Normal";

                if (posicion <= 3) {
                  prioridad = "Alta";
                } else if (posicion <= 10) {
                  prioridad = "Media";
                }

                return (
                  <tr key={producto.referencia}>
                    <td>
                      <span
                        className={`posicion-tabla ${
                          posicion <= 3 ? "top" : ""
                        }`}
                      >
                        {posicion}
                      </span>
                    </td>

                    <td>
                      <strong>{producto.referencia}</strong>
                    </td>

                    <td>
                      <strong className="valor-demanda">
                        {formatearNumero(producto.pronostico)}
                      </strong>

                      <span className="unidad-tabla">unidades</span>
                    </td>

                    <td>
                      {producto.inferior != null &&
                      producto.superior != null ? (
                        <span className="intervalo-tabla">
                          {formatearNumero(producto.inferior)}

                          {" — "}

                          {formatearNumero(producto.superior)}
                        </span>
                      ) : (
                        <span className="sin-intervalo">—</span>
                      )}
                    </td>

                    <td>
                      <span
                        className={`metodo-tabla ${
                          producto.metodo === "KRR" ? "krr" : ""
                        }`}
                      >
                        {producto.metodo || "N/D"}
                      </span>
                    </td>

                    <td>
                      <span
                        className={`prioridad-tabla ${prioridad.toLowerCase()}`}
                      >
                        {prioridad}
                      </span>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>

      {/* =====================================
          INTERPRETACIÓN
      ====================================== */}

      <div className="decisiones-interpretacion">
        <div className="interpretacion-header">
          <h3>¿Cómo interpretar esta información?</h3>

          <p>
            Utiliza estos indicadores como apoyo para orientar las decisiones
            comerciales y de planificación.
          </p>
        </div>

        <div className="interpretacion-grid">
          <div className="interpretacion-item">
            <div className="interpretacion-icono">1</div>

            <div>
              <strong>Prioriza las referencias</strong>

              <p>
                Las referencias ubicadas en las primeras posiciones concentran
                una mayor demanda proyectada.
              </p>
            </div>
          </div>

          <div className="interpretacion-item">
            <div className="interpretacion-icono">2</div>

            <div>
              <strong>Considera el intervalo</strong>

              <p>
                El rango inferior y superior permite evaluar distintos
                escenarios posibles de demanda.
              </p>
            </div>
          </div>

          <div className="interpretacion-item">
            <div className="interpretacion-icono">3</div>

            <div>
              <strong>Revisa el método utilizado</strong>

              <p>
                El método permite conocer cómo se obtuvo la estimación
                presentada.
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default Decisiones;
