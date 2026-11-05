# ==============================================================================
# Dockerfile para Frontend SPA (Vue 3 + Vite)
# Sistema de Control de Caja Universitaria - TecNM Campus Monclova
# ==============================================================================
FROM node:20-alpine

WORKDIR /app

# Copiar manifiesto de dependencias y restaurar
COPY package*.json ./
RUN npm install

# Copiar el código fuente
COPY . .

# Exponer el puerto configurado de Vite
EXPOSE 6030

# Arrancar el servidor de desarrollo en 0.0.0.0 y puerto 6030
CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0", "--port", "6030"]
