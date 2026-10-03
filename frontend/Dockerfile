# 1. Build the optimised production bundle.
FROM node:24 AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npx ng build

# 2. Serve it with nginx, which also forwards /api to the backend.
#    The nginx image fills ${API_URL} into the template on startup, so the same image works in every environment.
FROM nginx:alpine
ENV API_URL=http://backend:8080
COPY nginx/default.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html
EXPOSE 80
