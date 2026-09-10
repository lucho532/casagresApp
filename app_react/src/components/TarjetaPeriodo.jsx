import { formatearMes } from "../utils/formato";
import "../styles/TarjetaPeriodo.css";

function TarjetaPeriodo({
  mes,
  etiqueta = "Periodo mostrado",
  descripcion = "Datos proyectados para este mes",
  mesesDisponibles,
  onCambiarMes,
}) {
  const esEditable =
    typeof onCambiarMes === "function" &&
    Array.isArray(mesesDisponibles) &&
    mesesDisponibles.length > 0;

  return (
    <div className="tarjeta-periodo">
      <div className="tarjeta-periodo-icono">📅</div>

      <div>
        <span>{etiqueta}</span>

        {esEditable ? (
          <div className="tarjeta-periodo-select-envoltorio">
            <select
              className="tarjeta-periodo-select"
              value={mes || ""}
              onChange={(e) => onCambiarMes(e.target.value)}
              aria-label={etiqueta}
            >
              {mesesDisponibles.map((mesDisponible) => (
                <option key={mesDisponible.mes} value={mesDisponible.mes}>
                  {formatearMes(mesDisponible.mes)}
                </option>
              ))}
            </select>
          </div>
        ) : (
          <strong>{formatearMes(mes)}</strong>
        )}

        <small>{descripcion}</small>
      </div>
    </div>
  );
}

export default TarjetaPeriodo;
