# Se construye con el contexto en app_react/:
#
#   docker build -t casagres-frontend \
#     --build-arg VITE_API_URL=https://api.tu-dominio.com/api \
#     app_react/

# ============================================================
# Etapa 1: build de producción con Vite
# ============================================================
FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# Vite incrusta las variables VITE_* como texto plano en el bundle en
# tiempo de build, no se pueden cambiar después con variables de entorno
# del contenedor: por eso se reciben como build-arg, no como ENV.
ARG VITE_API_URL=http://localhost:5121/api
ENV VITE_API_URL=${VITE_API_URL}

RUN npm run build

# ============================================================
# Etapa 2: servir los archivos estáticos con nginx
# ============================================================
FROM nginx:1.27-alpine AS final

COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
