import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { PublicClientApplication } from "@azure/msal-browser";
import { MsalProvider } from "@azure/msal-react";
import { GoogleOAuthProvider } from "@react-oauth/google";

import App from "./App.jsx";
import { msalConfig, googleClientId } from "./authConfig";

import "./index.css";

const msalInstance = new PublicClientApplication(msalConfig);

// Microsoft no incluye ninguna foto en el id_token (a diferencia de Google,
// que sí la trae en su respuesta de perfil): para mostrarla hay que pedirla
// aparte a Microsoft Graph, con el access_token que ya se obtuvo en el mismo
// login (loginRequest ya pide el scope "User.Read"). Se hace todo del lado
// del navegador -sin pasar por el backend ni guardarla en la base de datos-
// convirtiendo la imagen a un data URL que dura lo que dure la sesión.
async function guardarFotoDePerfilMicrosoft(accessToken) {
  try {
    const respuesta = await fetch("https://graph.microsoft.com/v1.0/me/photo/$value", {
      headers: { Authorization: `Bearer ${accessToken}` },
    });

    if (!respuesta.ok) {
      // La causa más común: la cuenta de Microsoft no tiene ninguna foto
      // configurada (404). No es un error real, simplemente no hay nada
      // que mostrar.
      return;
    }

    const blob = await respuesta.blob();

    const dataUrl = await new Promise((resolve, reject) => {
      const lector = new FileReader();
      lector.onload = () => resolve(lector.result);
      lector.onerror = reject;
      lector.readAsDataURL(blob);
    });

    sessionStorage.setItem("foto_perfil_microsoft", dataUrl);
  } catch (error) {
    console.error("No fue posible obtener la foto de perfil de Microsoft:", error);
  }
}

async function iniciarAplicacion() {
  await msalInstance.initialize();

  const respuestaRedirect = await msalInstance.handleRedirectPromise();

  if (respuestaRedirect) {
    console.log("Login Microsoft completado correctamente.");

    sessionStorage.setItem("microsoft_id_token", respuestaRedirect.idToken);

    if (respuestaRedirect.accessToken) {
      await guardarFotoDePerfilMicrosoft(respuestaRedirect.accessToken);
    }
  }

  createRoot(document.getElementById("root")).render(
    <StrictMode>
      <MsalProvider instance={msalInstance}>
        <GoogleOAuthProvider clientId={googleClientId}>
          <App />
        </GoogleOAuthProvider>
      </MsalProvider>
    </StrictMode>,
  );
}

iniciarAplicacion();
