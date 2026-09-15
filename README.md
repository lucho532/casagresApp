# CASAGRES – Plataforma de Analítica de Ventas

## 📊 Descripción

**CASAGRES – Analítica de Ventas** es una plataforma orientada al análisis de información comercial y a la predicción de la demanda de productos.

La aplicación permite procesar históricos de ventas, analizar tendencias de compra y generar pronósticos de demanda futura, proporcionando información para apoyar la planificación comercial y la toma de decisiones.

El sistema integra un **frontend web**, una **API backend** y diferentes módulos de **procesamiento y predicción mediante Python**.

---

# 🎯 Objetivo

El objetivo principal de la plataforma es transformar los datos históricos de ventas en información útil para la toma de decisiones.

El sistema permite analizar:

* Comportamiento histórico de las ventas.
* Tendencias de compra.
* Demanda futura.
* Pronósticos por producto.
* Intervalos de predicción.
* Métodos utilizados para cada serie.
* Productos con mayor demanda proyectada.
* Productos que requieren mayor atención.

---

# 🚀 Funcionalidades

## 🏠 Inicio

La página principal proporciona una visión general de los resultados de la predicción.

Permite consultar:

* Periodo de análisis.
* Cantidad de productos.
* Demanda proyectada.
* Ranking de productos.
* Información resumida del pronóstico.

El periodo seleccionado funciona como un **selector global**, de forma que las diferentes secciones de la aplicación trabajan sobre el mismo periodo.

---

## 📈 Tendencias de compra

Permite analizar la evolución histórica de las ventas de los productos.

La sección muestra:

* Históricos de ventas.
* Evolución temporal.
* Tendencias de compra.
* Predicciones futuras.
* Comparación visual entre ventas reales y predicciones.

Las ventas históricas se muestran como una línea continua y las predicciones futuras se diferencian visualmente para facilitar su interpretación.

---

## 🔮 Demanda futura

Muestra la demanda proyectada para los periodos futuros.

Para cada producto se pueden consultar datos como:

* Referencia.
* Pronóstico.
* Método utilizado.
* Intervalo inferior.
* Intervalo superior.
* Ancho del intervalo.
* Información relacionada con el modelo utilizado.

---

## 🎯 Enfoque en decisiones

Esta sección transforma los resultados del modelo en información orientada a la toma de decisiones.

Incluye:

* Ranking de productos.
* Productos prioritarios.
* Demanda proyectada.
* Método de predicción.
* Intervalos de predicción.
* Indicadores de prioridad.
* Interpretación de los resultados.

El objetivo es facilitar la identificación de los productos que requieren mayor atención.

---

## 📦 Productos

Permite consultar las referencias disponibles dentro de los datos procesados.

La información de cada producto puede utilizarse posteriormente para analizar su comportamiento histórico y su demanda proyectada.

---

## 📊 Power BI

La plataforma dispone de una sección destinada a la visualización de información mediante **Power BI**.

Esta sección complementa los análisis realizados directamente dentro de la aplicación con dashboards y visualizaciones adicionales, permitiendo administrar (agregar y eliminar) los tableros disponibles.

---

## 👤 Administración de usuarios

Sección disponible únicamente para usuarios con rol **administrador**.

Permite:

* Consultar el listado de usuarios registrados.
* Cambiar el rol de un usuario (`admin`, `usuario`, `pendiente`).
* Activar o desactivar cuentas.
* Eliminar usuarios (los tokens de verificación/recuperación asociados se eliminan en cascada).

---

## ⏳ Cuenta pendiente de aprobación

Cuando un usuario nuevo se registra, su cuenta queda con el rol **pendiente** hasta que un administrador le asigna acceso. Mientras tanto, al iniciar sesión se le muestra una pantalla informativa en lugar del dashboard, evitando que vea información comercial sin autorización.

---

# 🔄 Actualización de datos

La aplicación incorpora un proceso de actualización que permite obtener nuevamente los datos y ejecutar todo el pipeline analítico.

El usuario puede seleccionar el horizonte de predicción:


Durante la ejecución se muestra:

* Estado del proceso.
* Porcentaje de progreso.
* Estado de la actualización.
* Errores, cuando se producen.

El sistema también controla que no se ejecuten simultáneamente varias actualizaciones.

---

# 🤖 Pipeline analítico

El procesamiento de los datos sigue un flujo compuesto por diferentes etapas:

```text
Informes históricos
       │
       ▼
Procesamiento y limpieza
       │
       ▼
Ventas procesadas
       │
       ▼
Adaptación de datos
       │
       ▼
Series mensuales
       │
       ▼
Preparación de series
       │
       ▼
Modelo de predicción
       │
       ▼
Pronósticos
       │
       ▼
Intervalos de predicción
       │
       ▼
API Backend
       │
       ▼
Dashboard React
```

---

# 🧠 Módulo de predicción

El proyecto incluye un módulo específico destinado a la generación de pronósticos.

Entre los componentes principales se encuentra **Kernel Ridge Regression (KRR)**, utilizado para realizar predicciones sobre las series de ventas.

El sistema también contempla estrategias alternativas para las series que no pueden ser procesadas directamente mediante el modelo principal.

Además, se generan intervalos de predicción para complementar las estimaciones.

---

# 📊 Resultados generados

El pipeline genera diferentes archivos utilizados posteriormente por el backend.

Entre los principales resultados se encuentran:

```text
pronostico.csv
pronostico_intervalos.csv
metodo_por_serie.csv
estado_aci.json
```

También se generan archivos intermedios relacionados con la preparación de los datos:

```text
df_preprocessed.csv

series_mensuales.csv
catalogo_series.csv
```

---

# 🏗️ Arquitectura

La aplicación está dividida principalmente en tres componentes:

```text
┌─────────────────────────────┐
│          FRONTEND           │
│        React + Vite         │
│                             │
│ Dashboard / Gráficos / UI  │
└──────────────┬──────────────┘
               │ HTTP / REST
               ▼
┌─────────────────────────────┐
│           BACKEND           │
│         ASP.NET Core        │
│                             │
│ API / Autenticación /       │
│ Servicios / Actualización   │
└──────────────┬──────────────┘
               │
               ▼
┌─────────────────────────────┐
│      MÓDULO ANALÍTICO       │
│           Python            │
│                             │
│ Procesamiento / Series /    │
│ Machine Learning /          │
│ Pronósticos                 │
└─────────────────────────────┘
```

---

# 🛠️ Tecnologías utilizadas

## Frontend

* **React 19.2.8** — Desarrollo de la interfaz.
* **Vite 8.2.2** — Herramienta de desarrollo y construcción.
* **Axios 1.20.0** — Comunicación con la API.
* **Recharts 3.10.1** — Gráficos y visualizaciones.
* **Microsoft MSAL** — Autenticación con Microsoft.

## Backend

* **.NET 9**
* **ASP.NET Core**
* **Entity Framework Core 9**
* **PostgreSQL** (Npgsql)
* **JWT** — autenticación basada en tokens.
* **Microsoft Graph API** (vía `HttpClient`, sin el SDK) — acceso a OneDrive.
* **MSAL.NET** — autenticación de la cuenta de Microsoft usada para OneDrive.
* **ClosedXML**
* **BCrypt**

## Analítica y Machine Learning

* **Python**
* **NumPy 2.5.2**
* **Pandas 3.0.5**
* **Scikit-learn 1.9.0**
* **SciPy 1.18.1**
* **OpenPyXL 3.1.5**
* **Requests 2.34.2**

---

# 📦 Dependencias

## Frontend

Dependencias principales:

```text
@azure/msal-browser       5.21.0
@azure/msal-react         5.7.0
@react-oauth/google       0.13.5
axios                     1.20.0
react                     19.2.8
react-dom                 19.2.8
recharts                  3.10.1
```

Dependencias de desarrollo:

```text
@eslint/js
@testing-library/jest-dom
@testing-library/react
@testing-library/user-event
@types/react
@types/react-dom
@vitejs/plugin-react
eslint
eslint-plugin-react-hooks
eslint-plugin-react-refresh
globals
jsdom
vite
vitest
```

---

## Backend

Paquetes principales:

```text
BCrypt.Net-Next                                  4.0.3
ClosedXML                                        0.105.1
Microsoft.AspNetCore.Authentication.JwtBearer   9.0.8
Microsoft.AspNetCore.OpenApi                    9.0.19
Microsoft.EntityFrameworkCore.Tools              9.0.19
Microsoft.Identity.Client                        4.88.0
Microsoft.Identity.Client.Extensions.Msal       4.88.0
Microsoft.IdentityModel.Protocols.OpenIdConnect 8.22.0
Npgsql.EntityFrameworkCore.PostgreSQL            9.0.4
```

Framework:

```text
.NET 9.0
```

---

## Python

Dependencias principales:

```text
numpy==2.5.2
pandas==3.0.5
scikit-learn==1.9.0
scipy==1.18.1
openpyxl==3.1.5
requests==2.34.2
bcrypt==5.0.0
psycopg2-binary==2.9.13
```

Otras dependencias instaladas en el entorno:

```text
certifi==2026.7.22
charset-normalizer==3.5.1
cloudpickle==3.1.2
docopt==0.6.2
et_xmlfile==2.0.0
idna==3.19
joblib==1.6.0
narwhals==2.25.0
pipreqs==0.4.13
python-dateutil==2.9.0.post0
six==1.17.0
threadpoolctl==3.6.0
tzdata==2026.3
urllib3==2.7.0
yarg==0.1.10
```

---

# 🔐 Autenticación y seguridad

La aplicación incorpora mecanismos de autenticación y autorización.

## Inicio de sesión y registro

* El registro solo requiere **nombre, correo electrónico y contraseña** (no existe un campo de usuario independiente).
* El inicio de sesión se realiza con **correo electrónico y contraseña**.
* También es posible iniciar sesión con **Google** o **Microsoft** (OAuth).
* Tras el registro se envía un **correo de verificación**; la cuenta debe verificarse antes de poder iniciar sesión.
* Existe un flujo de **recuperación de contraseña** mediante un enlace enviado por correo (con expiración).

## Roles y control de acceso

* **admin** — acceso completo, incluida la administración de usuarios.
* **usuario** — acceso a los datos y análisis de la plataforma.
* **pendiente** — rol asignado automáticamente a las cuentas nuevas hasta que un administrador las aprueba; no tiene acceso a datos comerciales.

## Seguridad

* JWT para autenticación mediante tokens (con revalidación del usuario en cada solicitud, por si su cuenta fue desactivada o eliminada).
* BCrypt para el hash de contraseñas.
* Microsoft Identity / MSAL para la integración con OneDrive y Microsoft Graph.
* Envío de correo transaccional (verificación de cuenta y recuperación de contraseña) mediante SMTP.

La interfaz adapta las opciones disponibles según el rol del usuario.

---

# 🔌 API Backend

El frontend se comunica con el backend mediante una API REST.

Entre los servicios disponibles actualmente se encuentran:

```text
# Pronóstico
GET  /api/Pronostico/pronostico
GET  /api/Pronostico/pronostico-intervalos
GET  /api/Pronostico/metodos
GET  /api/Pronostico/dashboard
GET  /api/Pronostico/historico

# Actualización de datos
GET  /api/Actualizacion/estado
POST /api/Actualizacion/ejecutar
POST /api/Actualizacion/actualizar
GET  /api/Actualizacion/probar-onedrive

# Productos
GET  /api/Productos

# Power BI
GET    /api/PowerBi
POST   /api/PowerBi
DELETE /api/PowerBi/{id}

# Autenticación
GET  /api/auth/perfil
POST /api/auth/login
POST /api/auth/registro
POST /api/auth/microsoft
POST /api/auth/google
POST /api/auth/solicitar-reset
POST /api/auth/restablecer-password
POST /api/auth/verificar-email
POST /api/auth/reenviar-verificacion

# Administración (solo rol admin)
GET    /api/Administracion/usuarios
PUT    /api/Administracion/usuarios/{id}/rol
PUT    /api/Administracion/usuarios/{id}/estado
DELETE /api/Administracion/usuarios/{id}
```

La API proporciona al frontend la información necesaria para construir los dashboards y controlar los procesos de actualización.

---

# 🔄 Flujo de actualización

Cuando el usuario ejecuta una actualización, el backend inicia el pipeline completo:

```text
1. Obtener archivos de datos
          ↓
2. Procesar informes
          ↓
3. Generar datos limpios
          ↓
4. Adaptar datos al formato analítico
          ↓
5. Preparar series mensuales
          ↓
6. Ejecutar predicción
          ↓
7. Generar intervalos
          ↓
8. Generar archivos de resultados
          ↓
9. Actualizar información disponible para la API
```

El usuario puede seleccionar el horizonte de predicción antes de iniciar el proceso.

---

# ⚙️ Requisitos

El backend es **multiplataforma** (probado en Windows y en Linux, incluyendo contenedores Docker). Para ejecutar el proyecto se requiere disponer de:

* .NET 9 SDK.
* Node.js y npm.
* Python 3 con un entorno virtual.
* PostgreSQL.
* Acceso a las fuentes de datos utilizadas por el pipeline (carpeta compartida de OneDrive).
* Configuración correspondiente de la base de datos (`ConnectionStrings:CadenaPostgres`).
* Configuración de autenticación y servicios externos (JWT, Google/Microsoft OAuth, SMTP) mediante `appsettings.json` o variables de entorno.

> En Linux, si no hay un keyring disponible (por ejemplo dentro de un contenedor), el caché de tokens de Microsoft se guarda sin cifrar en disco como respaldo automático.

---

# ▶️ Ejecución

## Backend

Desde:

```text
Casagres.API/
```

ejecutar:

```powershell
dotnet restore
dotnet run
```

---

## Frontend

Desde:

```text
app_react/
```

instalar las dependencias:

```powershell
npm install
```

Ejecutar en modo desarrollo:

```powershell
npm run dev
```

---

## Python

El proyecto utiliza un entorno virtual ubicado en:

```text
.venv/
```

Para activar el entorno desde PowerShell:

```powershell
.\.venv\Scripts\Activate.ps1
```

Para consultar las dependencias:

```powershell
python -m pip freeze
```

---

# 🧪 Scripts del frontend

El proyecto React incluye los siguientes comandos:

```text
npm run dev
```

Inicia el servidor de desarrollo.

```text
npm run build
```

Genera la versión de producción.

```text
npm run lint
```

Ejecuta las comprobaciones de ESLint.

```text
npm run preview
```

Permite visualizar localmente la versión construida.

---

# 🐳 Despliegue con Docker

La aplicación puede empaquetarse y levantarse completa (backend + frontend)
con Docker, sin instalar .NET, Node ni Python en la máquina destino.

## Requisitos

* Docker y Docker Compose.
* Una base de datos PostgreSQL accesible desde donde corra el contenedor
  (por ejemplo, un proyecto de Supabase).

## Puesta en marcha

1. Copiar el archivo de variables de entorno de ejemplo y completarlo:

   ```powershell
   cp .env.example .env
   ```

   Ahí se configuran, entre otras cosas, la cadena de conexión a Postgres,
   la clave del JWT, las credenciales SMTP y la URL pública del frontend.

2. Levantar todo:

   ```powershell
   docker compose up -d --build
   ```

   Esto construye dos imágenes:

   * **`backend`** — API en .NET 9, con Python 3.12 instalado dentro de la
     misma imagen para poder ejecutar el pipeline analítico. Expone el
     puerto `8090` (mapeado al `8080` interno).
   * **`frontend`** — build de producción de React servido con nginx. La
     URL de la API se incrusta en el build (variable `VITE_API_URL`), así
     que si cambia hay que reconstruir la imagen. Expone el puerto `8081`.

3. La primera vez, hay que crear las tablas en la base de datos con las
   migraciones de Entity Framework (una sola vez, no en cada despliegue):

   ```powershell
   cd Casagres.API
   dotnet ef database update
   ```

## Datos que persisten entre despliegues

El `docker-compose.yml` define volúmenes para lo que no debe perderse al
recrear los contenedores:

* `casagres_datos` — informes de ventas y salidas del pipeline.
* `casagres_msal_cache` — sesión de la cuenta de Microsoft usada para
  OneDrive (si no se persiste, hay que volver a iniciar sesión cada vez).
* `casagres_dataprotection_keys` — llaves internas de ASP.NET Core.

## Notas

* El backend corre en modo de globalización invariante
  (`DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1`) porque el código ya parsea
  números con `CultureInfo.InvariantCulture`, evitando así depender de
  paquetes de ICU específicos de la distribución de Linux usada.
* Si Supabase (u otro proveedor) solo ofrece el *connection pooler* en modo
  transacción (puerto 6543), usar en su lugar el *session pooler* (puerto
  5432) para correr las migraciones — el modo transacción puede fallar al
  ejecutar sentencias DDL como `CREATE TABLE`.

## Despliegue gratuito en Render

El repositorio incluye un `render.yaml` (Blueprint de Render) que define
backend y frontend juntos:

1. En el dashboard de Render: **New → Blueprint**, y seleccionar este
   repositorio de GitHub.
2. Render pide los valores de las variables marcadas como secretas
   (`ConnectionStrings__CadenaPostgres`, `Jwt__Key`, credenciales SMTP,
   `VITE_API_URL`, etc.) — son las mismas de `.env.example`.
3. Una vez desplegado el frontend, actualizar `Frontend__Url` (backend) y
   `VITE_API_URL` (frontend) con las URLs reales que asigna Render
   (`https://casagres-frontend.onrender.com`, etc.) y volver a desplegar.

**Limitaciones del plan gratuito a tener en cuenta:**

* El servicio del backend se "duerme" tras 15 minutos sin tráfico; la
  primera solicitud después de eso tarda unos 30-50 segundos en responder.
* El plan gratuito no incluye disco persistente: la carpeta de datos
  (`/app/proyecto_icmd`) y la sesión de Microsoft (caché de OneDrive) se
  reinician en cada despliegue. En la práctica esto significa que, tras
  cada deploy, hay que volver a iniciar sesión con Microsoft y volver a
  ejecutar "Actualizar datos" para regenerar el pronóstico.
* El sitio estático del frontend, en cambio, no se duerme (los sitios
  estáticos de Render son siempre gratuitos y no tienen ese límite).

---

# 🧪 Pruebas automatizadas

El proyecto cuenta con pruebas automatizadas tanto en el backend como en el frontend.

## Backend (xUnit + Moq)

Desde `Casagres.API/`:

```powershell
dotnet test
```

## Frontend (Vitest + React Testing Library)

Desde `app_react/`:

```powershell
npm test
```

---

# 📈 Flujo de información

El flujo completo de información de CASAGRES puede resumirse de la siguiente manera:

```text
                DATOS DE VENTAS
                       │
                       ▼
              ┌─────────────────┐
              │   Procesamiento  │
              │     Python       │
              └────────┬────────┘
                       │
                       ▼
              SERIES DE VENTAS
                       │
                       ▼
              ┌─────────────────┐
              │    MODELOS ML   │
              │      KRR        │
              └────────┬────────┘
                       │
                       ▼
                PRONÓSTICOS
                       │
             ┌─────────┴─────────┐
             ▼                   ▼
       Intervalos             Métodos
       de predicción         utilizados
             │                   │
             └─────────┬─────────┘
                       ▼
                 BACKEND API
                       │
                       ▼
                FRONTEND REACT
                       │
             ┌─────────┼─────────┐
             ▼         ▼         ▼
          Dashboard  Gráficos  Decisiones
```

---

# 📌 Estado del proyecto

🚧 **Proyecto en desarrollo.**

La plataforma se encuentra en evolución y puede incorporar nuevas funcionalidades, modelos de predicción, indicadores y mejoras en la visualización de los resultados.

---

# 🔮 Posibles mejoras futuras

Entre las posibles líneas de evolución del proyecto se encuentran:

* Incorporación de nuevos modelos de predicción.
* Mejora de la selección automática de modelos.
* Ampliación de indicadores comerciales.
* Nuevas visualizaciones.
* Análisis comparativo entre periodos.
* Mejoras en la interpretación de los pronósticos.
* Optimización del pipeline de procesamiento.
* Ampliación de las funcionalidades administrativas.
* Mejor integración con Power BI.

---

# 👨‍💻 Proyecto

**CASAGRES – Plataforma de Analítica de Ventas**

Sistema destinado al procesamiento de información comercial, análisis de ventas, predicción de demanda y apoyo a la toma de decisiones.
