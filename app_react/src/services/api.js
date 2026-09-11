import axios from "axios";

const api = axios.create({
  baseURL: "http://localhost:5121/api",
});


/* =========================================================
   INTERCEPTOR DE PETICIONES
   Agrega automáticamente el JWT
   ========================================================= */

api.interceptors.request.use(
  (config) => {
    const token = sessionStorage.getItem("token");

    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);


/* =========================================================
   INTERCEPTOR DE RESPUESTAS
   Cierra la sesión cuando el JWT expira
   ========================================================= */

api.interceptors.response.use(
  (respuesta) => {
    return respuesta;
  },

  (error) => {
    const url = error.config?.url || "";

    // No cerrar sesión si el 401 viene del login
    const esLogin = url.toLowerCase().includes("/auth/login");

    if (
      error.response?.status === 401 &&
      !esLogin
    ) {
      sessionStorage.removeItem("token");
      window.location.reload();
    }

    return Promise.reject(error);
  }
);


/* =========================================================
   DASHBOARD
   ========================================================= */

export const obtenerDashboard = async () => {
  const respuesta = await api.get("/Pronostico/dashboard");
  return respuesta.data;
};

export const obtenerHistorico = async (referencia) => {
  const respuesta = await api.get("/Pronostico/historico", {
    params: {
      referencia,
    },
  });

  return respuesta.data;
};


/* =========================================================
   ACTUALIZACIÓN
   ========================================================= */

export const actualizarDatos = async (horizonte = 1) => {
  const respuesta = await api.post("/Actualizacion/actualizar", null, {
    params: {
      horizonte,
    },
  });

  return respuesta.data;
};

export const obtenerEstadoActualizacion = async () => {
  const respuesta = await api.get("/Actualizacion/estado");
  return respuesta.data;
};


/* =========================================================
   POWER BI
   ========================================================= */

export const obtenerTablerosPowerBi = async () => {
  const respuesta = await api.get("/PowerBi");
  return respuesta.data;
};

export const agregarTableroPowerBi = async (nombre, url) => {
  const respuesta = await api.post("/PowerBi", {
    nombre,
    url,
  });

  return respuesta.data;
};

export const eliminarTableroPowerBi = async (id) => {
  const respuesta = await api.delete(`/PowerBi/${id}`);

  return respuesta.data;
};


/* =========================================================
   PRODUCTOS
   ========================================================= */

export const obtenerProductos = async () => {
  const respuesta = await api.get("/Productos");
  return respuesta.data;
};


/* =========================================================
   AUTENTICACIÓN
   ========================================================= */

export const iniciarSesion = async (email, password) => {
  const respuesta = await api.post("/auth/login", {
    email,
    password,
  });

  return respuesta.data;
};

export const obtenerPerfil = async () => {
  const respuesta = await api.get("/auth/perfil");
  return respuesta.data;
};


export const registrarUsuario = async (
  password,
  nombre,
  email
) => {
  const respuesta = await api.post("/auth/registro", {
    password,
    nombre,
    email,
  });

  return respuesta.data;
};

export const obtenerUsuarios = async () => {
  const respuesta = await api.get("/Administracion/usuarios");
  return respuesta.data;
};


export const cambiarRol = async (id, rol) => {
  const respuesta = await api.put(
    `/Administracion/usuarios/${id}/rol`,
    {
      rol,
    }
  );

  return respuesta.data;
};

export const cambiarEstado = async (id, activo) => {
  const respuesta = await api.put(
    `/Administracion/usuarios/${id}/estado`,
    {
      activo,
    }
  );

  return respuesta.data;
};

export const eliminarUsuario = async (id) => {
  const respuesta = await api.delete(`/Administracion/usuarios/${id}`);

  return respuesta.data;
};

export const iniciarSesionMicrosoft = async (idToken) => {
  const respuesta = await api.post("/auth/microsoft", {
    idToken,
  });

  return respuesta.data;
};

export const iniciarSesionGoogle = async (accessToken) => {
  const respuesta = await api.post("/auth/google", {
    accessToken,
  });

  return respuesta.data;
};

export const solicitarResetPassword = async (email) => {
  const respuesta = await api.post("/auth/solicitar-reset", {
    email,
  });

  return respuesta.data;
};

export const restablecerPassword = async (token, nuevaPassword) => {
  const respuesta = await api.post("/auth/restablecer-password", {
    token,
    nuevaPassword,
  });

  return respuesta.data;
};

export const verificarEmail = async (token) => {
  const respuesta = await api.post("/auth/verificar-email", {
    token,
  });

  return respuesta.data;
};

export const reenviarVerificacion = async (email) => {
  const respuesta = await api.post("/auth/reenviar-verificacion", {
    email,
  });

  return respuesta.data;
};


export default api;