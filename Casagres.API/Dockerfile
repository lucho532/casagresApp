# Este Dockerfile se construye con el contexto en la RAÍZ del repositorio, no
# dentro de Casagres.API/, porque necesita acceso a requirements.txt:
#
#   docker build -f Casagres.API/Dockerfile -t casagres-api .

# ============================================================
# Etapa 1: compilar y publicar el backend .NET
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Casagres.API/Casagres.API.csproj Casagres.API/
RUN dotnet restore Casagres.API/Casagres.API.csproj

COPY Casagres.API/ Casagres.API/
RUN dotnet publish Casagres.API/Casagres.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ============================================================
# Etapa 2: imagen final de ejecución
# ============================================================
# Se parte de la imagen oficial de Python (no de la de .NET) porque numpy
# 2.5.2 requiere Python >= 3.12, y el "python3" que trae por apt la imagen
# de .NET (Debian bookworm) es 3.11. El runtime de ASP.NET Core se instala
# encima con el script oficial de Microsoft, que no depende de qué versión
# de Debian se use.
FROM python:3.12-slim AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl ca-certificates \
    && rm -rf /var/lib/apt/lists/*

RUN curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
    && bash /tmp/dotnet-install.sh --channel 9.0 --runtime aspnetcore --install-dir /usr/share/dotnet \
    && rm /tmp/dotnet-install.sh
ENV PATH="/usr/share/dotnet:${PATH}"

COPY requirements.txt /tmp/requirements.txt
RUN pip install --no-cache-dir -r /tmp/requirements.txt \
    && rm /tmp/requirements.txt

# Binarios publicados del backend
COPY --from=build /app/publish .

# Scripts del pipeline analítico (no se publican junto con el backend porque
# no son código .NET, así que se copian aparte en la misma ruta relativa que
# usa ActualizacionService: <Python:Proyecto>/src/...)
COPY Casagres.API/src ./src

# El código ya parsea números con CultureInfo.InvariantCulture, así que no
# depende de ICU: se corre en modo "invariant" para no tener que instalar
# los paquetes de globalización de Debian (cuyo nombre de paquete cambia
# entre versiones de Debian).
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 \
    Python__Ejecutable=/usr/local/bin/python3 \
    Python__Proyecto=/app \
    Rutas__CarpetaDatos=/app/proyecto_icmd

# Datos de negocio (informes de ventas, salidas de la predicción): deben
# persistir entre despliegues, así que van en un volumen, no en la imagen.
VOLUME ["/app/proyecto_icmd"]

# Configuración obligatoria en tiempo de ejecución (sin valores por defecto
# porque son secretos o dependen del entorno):
#   ConnectionStrings__CadenaPostgres
#   Jwt__Key
#   OneDrive__CarpetaCompartidaUrl
#   Smtp__Host / Smtp__Puerto / Smtp__Usuario / Smtp__Password / Smtp__RemitenteNombre
#   Frontend__Url

EXPOSE 8080

ENTRYPOINT ["dotnet", "Casagres.API.dll"]
