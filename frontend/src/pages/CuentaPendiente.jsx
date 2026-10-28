import "../styles/CuentaPendiente.css";

function CuentaPendiente({ onCerrarSesion }) {
  return (
    <div className="cuenta-pendiente-pagina">
      <div className="cuenta-pendiente-tarjeta">
        <div className="cuenta-pendiente-icono">⏳</div>

        <h1>Cuenta pendiente de aprobación</h1>

        <p>
          Tu cuenta se creó correctamente, pero todavía no tienes acceso a
          los datos de CASAGRES. Un administrador debe aprobarla primero.
        </p>

        <p className="cuenta-pendiente-nota">
          Esta pantalla se actualiza sola en cuanto te aprueben — no hace
          falta que hagas nada más.
        </p>

        <button
          type="button"
          className="cuenta-pendiente-boton"
          onClick={onCerrarSesion}
        >
          Cerrar sesión
        </button>
      </div>
    </div>
  );
}

export default CuentaPendiente;
