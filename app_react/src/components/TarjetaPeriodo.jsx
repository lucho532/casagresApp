import { formatearMes } from "../utils/formato";
import "../styles/TarjetaPeriodo.css";

function TarjetaPeriodo({
  mes,
  etiqueta = "Periodo mostrado",
  descripcion = "Datos proyectados para este mes",
}) {
  return (
    <div className="tarjeta-periodo">
      <div className="tarjeta-periodo-icono">📅</div>

      <div>
        <span>{etiqueta}</span>
        <strong>{formatearMes(mes)}</strong>
        <small>{descripcion}</small>
      </div>
    </div>
  );
}

export default TarjetaPeriodo;
