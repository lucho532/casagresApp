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

Esta sección complementa los análisis realizados directamente dentro de la aplicación con dashboards y visualizaciones adicionales.

---

# 🔄 Actualización de datos

La aplicación incorpora un proceso de actualización que permite obtener nuevamente los datos y ejecutar todo el pipeline analítico.

El usuario puede seleccionar el horizonte de predicción:

* **1 mes**
* **3 meses**
* **6 meses**
* **12 meses**

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
* **SQL Server**
* **PostgreSQL**
* **JWT**
* **Microsoft Graph**
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
axios                     1.20.0
react                     19.2.8
react-dom                 19.2.8
recharts                  3.10.1
```

Dependencias de desarrollo:

```text
@eslint/js
@types/react
@types/react-dom
@vitejs/plugin-react
eslint
eslint-plugin-react-hooks
eslint-plugin-react-refresh
globals
vite
```

---

## Backend

Paquetes principales:

```text
BCrypt.Net-Next                                  4.0.3
ClosedXML                                        0.105.1
Microsoft.AspNetCore.Authentication.JwtBearer   9.0.8
Microsoft.AspNetCore.OpenApi                    9.0.19
Microsoft.EntityFrameworkCore.SqlServer         9.0.19
Microsoft.EntityFrameworkCore.Tools              9.0.19
Microsoft.Graph                                  6.5.0
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

El backend utiliza:

* JWT para autenticación mediante tokens.
* BCrypt para el procesamiento seguro de contraseñas.
* Microsoft Identity / MSAL para integración con servicios de Microsoft.
* Control de acceso mediante roles.

La interfaz adapta las opciones disponibles según el rol del usuario.

---

# 🔌 API Backend

El frontend se comunica con el backend mediante una API REST.

Entre los servicios utilizados actualmente se encuentran:

```text
GET  /api/Pronostico/dashboard
GET  /api/Pronostico/historico
POST /api/Actualizacion/actualizar
GET  /api/Actualizacion/estado
GET  /api/Productos
POST /api/auth/login
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

Para ejecutar el proyecto se requiere disponer de:

* Windows.
* .NET 9 SDK.
* Node.js y npm.
* Python.
* Entorno virtual Python.
* Acceso a las fuentes de datos utilizadas por el pipeline.
* Configuración correspondiente de la base de datos.
* Configuración de autenticación y servicios externos cuando corresponda.

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
