import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { PublicClientApplication } from "@azure/msal-browser";
import { MsalProvider } from "@azure/msal-react";

import App from "./App.jsx";
import { msalConfig } from "./authConfig";

import "./index.css";

const msalInstance = new PublicClientApplication(msalConfig);

async function iniciarAplicacion() {
  await msalInstance.initialize();

  const respuestaRedirect = await msalInstance.handleRedirectPromise();

  if (respuestaRedirect) {
    console.log("Login Microsoft completado correctamente.");

    sessionStorage.setItem("microsoft_id_token", respuestaRedirect.idToken);
  }

  createRoot(document.getElementById("root")).render(
    <StrictMode>
      <MsalProvider instance={msalInstance}>
        <App />
      </MsalProvider>
    </StrictMode>,
  );
}

iniciarAplicacion();
