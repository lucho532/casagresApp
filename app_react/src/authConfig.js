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