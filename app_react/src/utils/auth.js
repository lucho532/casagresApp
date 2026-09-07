export const obtenerToken = () => {
  return localStorage.getItem("token");
};


export const obtenerRol = () => {
  const token = obtenerToken();

  if (!token) {
    return null;
  }

  try {
    const payload = token.split(".")[1];

    const payloadDecodificado = JSON.parse(
      atob(payload.replace(/-/g, "+").replace(/_/g, "/"))
    );

    return (
      payloadDecodificado[
        "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
      ] || null
    );

  } catch (error) {
    console.error("No fue posible obtener el rol del usuario:", error);
    return null;
  }
};
