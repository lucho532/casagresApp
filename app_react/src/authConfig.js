export const msalConfig = {
  auth: {
    clientId: "c21b50d1-67bf-4da2-a167-25e5d3625973",
    authority: "https://login.microsoftonline.com/common",
    redirectUri: "http://localhost:5173",
  },
};

export const loginRequest = {
  scopes: ["User.Read"],
};

// TODO: reemplazar por el Client ID real generado en Google Cloud Console
// (APIs y servicios > Credenciales > ID de cliente de OAuth 2.0 > Aplicación
// web), con http://localhost:5173 registrado como origen autorizado de
// JavaScript. Debe coincidir con el Client ID configurado en el backend
// (Casagres.API/Services/AuthService.cs).
export const googleClientId =
  "1089055824770-updof067c782c8m289v2ei9i7ihpn0a4.apps.googleusercontent.com";