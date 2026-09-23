export const obtenerToken = () => {
  return sessionStorage.getItem("token");
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

// La foto de perfil de Microsoft no viaja en el JWT (se obtiene aparte,
// directo del navegador contra Microsoft Graph; ver main.jsx) y se guarda
// como data URL en sessionStorage, no en el token.
export const obtenerFotoPerfilMicrosoft = () => {
  return sessionStorage.getItem("foto_perfil_microsoft");
};

export const obtenerNombre = () => {
  const token = obtenerToken();

  if (!token) {
    return null;
  }

  try {
    const payload = token.split(".")[1];

    const base64 = payload
      .replace(/-/g, "+")
      .replace(/_/g, "/");

    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split("")
        .map(
          (caracter) =>
            "%" +
            ("00" + caracter.charCodeAt(0).toString(16)).slice(-2)
        )
        .join("")
    );

    const payloadDecodificado = JSON.parse(jsonPayload);

    return payloadDecodificado["nombre"] || null;
  } catch (error) {
    console.error(
      "No fue posible obtener el nombre del usuario:",
      error
    );

    return null;
  }
};
