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
    const token = localStorage.getItem("token");

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
      localStorage.removeItem("token");
      window.location.reload();
    }

    return Promise.reject(error);
  }
);


/* =========================================================
   DASHBOARD
   ========================================================= */

export const obtenerDashboard = async () => {
  const respuesta = await api.get("/Ventas/dashboard");
  return respuesta.data;
};

export const obtenerHistorico = async (referencia) => {
  const respuesta = await api.get("/Ventas/historico", {
    params: {
      referencia,
    },
  });

  return respuesta.data;
};


/* =========================================================
   PIPELINE
   ========================================================= */

export const actualizarDatos = async (horizonte = 1) => {
  const respuesta = await api.post("/Pipeline/actualizar", null, {
    params: {
      horizonte,
    },
  });

  return respuesta.data;
};

export const obtenerEstadoActualizacion = async () => {
  const respuesta = await api.get("/Pipeline/estado");
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

export const iniciarSesion = async (usuario, password) => {
  const respuesta = await api.post("/auth/login", {
    usuario,
    password,
  });

  return respuesta.data;
};


export const registrarUsuario = async (
  usuario,
  password,
  nombre,
  email
) => {
  const respuesta = await api.post("/auth/registro", {
    usuario,
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

export const iniciarSesionMicrosoft = async (idToken) => {
  const respuesta = await api.post("/auth/microsoft", {
    idToken,
  });

  return respuesta.data;
};




export default api;