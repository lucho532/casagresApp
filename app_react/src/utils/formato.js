// Muestra "-" cuando el valor no está disponible.
export const formatearNumero = (numero) => {
  if (numero == null) {
    return "-";
  }

  return Math.round(Number(numero)).toLocaleString("es-CO");
};

// Trata los valores ausentes como 0 en vez de mostrar "-".
export const formatearCantidad = (numero) => {
  return Math.round(Number(numero || 0)).toLocaleString("es-CO");
};

export const formatearMes = (mes) => {
  if (!mes) {
    return "-";
  }

  const fecha = new Date(`${mes}T00:00:00`);

  return fecha.toLocaleDateString("es-CO", {
    month: "long",
    year: "numeric",
  });
};
