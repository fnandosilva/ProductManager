# syntax=docker/dockerfile:1

# ---- Build the Angular app ----
FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# ---- Serve the compiled app with nginx ----
FROM nginx:1.27-alpine AS runtime

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/product-manager-client/browser /usr/share/nginx/html

EXPOSE 80
