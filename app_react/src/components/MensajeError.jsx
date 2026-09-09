function MensajeError({ mensaje, claseAdicional = "" }) {
  return (
    <div className={`pagina ${claseAdicional}`.trim()}>
      <div className="mensaje-error">{mensaje}</div>
    </div>
  );
}

export default MensajeError;
