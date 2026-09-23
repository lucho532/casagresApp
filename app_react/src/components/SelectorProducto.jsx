import { useMemo, useState } from "react";
import "../styles/SelectorProducto.css";

function SelectorProducto({
  productos,
  valorSeleccionado,
  onSeleccionar,
  etiqueta = "Producto",
  obtenerEtiquetaProducto = (producto) => producto.referencia,
}) {
  const [mostrarBusqueda, setMostrarBusqueda] = useState(false);
  const [busqueda, setBusqueda] = useState("");

  const productosFiltrados = useMemo(() => {
    const texto = busqueda.trim().toLowerCase();

    if (!texto) {
      return productos;
    }

    return productos.filter((producto) =>
      obtenerEtiquetaProducto(producto).toString().toLowerCase().includes(texto),
    );
  }, [productos, busqueda, obtenerEtiquetaProducto]);

  const alternarBusqueda = () => {
    setMostrarBusqueda((actual) => !actual);

    if (mostrarBusqueda) {
      setBusqueda("");
    }
  };

  const seleccionarPrimerResultado = () => {
    if (productosFiltrados.length > 0) {
      onSeleccionar(productosFiltrados[0].referencia);
      setBusqueda("");
      setMostrarBusqueda(false);
    }
  };

  return (
    <div className="selector-producto">
      <label>{etiqueta}</label>

      <div className="selector-producto-control">
        <select
          value={valorSeleccionado}
          onChange={(e) => onSeleccionar(e.target.value)}
        >
          {productosFiltrados.map((producto) => (
            <option key={producto.referencia} value={producto.referencia}>
              {obtenerEtiquetaProducto(producto)}
            </option>
          ))}
        </select>

        <button
          type="button"
          className={`boton-busqueda ${mostrarBusqueda ? "activo" : ""}`}
          onClick={alternarBusqueda}
          title="Buscar referencia"
        >
          🔍
        </button>
      </div>

      {mostrarBusqueda && (
        <div className="campo-busqueda-producto">
          <input
            type="text"
            placeholder="Buscar referencia..."
            value={busqueda}
            onChange={(e) => setBusqueda(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter") {
                seleccionarPrimerResultado();
              }
            }}
            autoFocus
          />

          {busqueda && (
            <span className="resultado-busqueda">
              {productosFiltrados.length} resultado
              {productosFiltrados.length !== 1 ? "s" : ""}
            </span>
          )}
        </div>
      )}
    </div>
  );
}

export default SelectorProducto;
