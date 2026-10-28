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

// Iniciales para el avatar por defecto (antes de subir una foto): la
// primera letra del primer nombre y la primera del último, como "Juan
// Pérez" -> "JP". Con una sola palabra, usa solo esa inicial.
export const obtenerIniciales = (nombre) => {
  if (!nombre?.trim()) {
    return "";
  }

  const palabras = nombre.trim().split(/\s+/);

  const primera = palabras[0][0];
  const ultima = palabras.length > 1 ? palabras[palabras.length - 1][0] : "";

  return (primera + ultima).toUpperCase();
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
