function PowerBI() {
  const powerBiUrl =
    "https://app.powerbi.com/view?r=eyJrIjoiNWU2NzRjYzItYzkwOS00MDAzLTk4MTItYzJkMzE2YTNiMjNhIiwidCI6IjU3N2ZjMWQ4LTA5MjItNDU4ZS04N2JmLWVjNGY0NTVlYjYwMCIsImMiOjR9";

  return (
    <div className="pagina powerbi">
      {/* =====================================
          ENCABEZADO
      ====================================== */}

      <div className="pagina-encabezado powerbi-encabezado">
        <div>
          <span className="powerbi-etiqueta">INTELIGENCIA DE NEGOCIO</span>

          <h2>Power BI</h2>

          <p>Consulta los informes y análisis interactivos de CASAGRES.</p>
        </div>
      </div>

      {/* =====================================
          INFORME POWER BI
      ====================================== */}

      <div className="powerbi-contenedor-real">
        <iframe
          title="Informe Power BI CASAGRES"
          src={powerBiUrl}
          frameBorder="0"
          allowFullScreen
        />
      </div>
    </div>
  );
}

export default PowerBI;
