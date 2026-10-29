function EstadoCargando({ mensaje, claseAdicional = "" }) {
  return (
    <div className={`pagina ${claseAdicional}`.trim()}>
      <div className="estado-cargando">
        <div className="spinner" />
        <p>{mensaje}</p>
      </div>
    </div>
  );
}

export default EstadoCargando;
