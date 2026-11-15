# Build context: web/bookstore-web.  docker build -t bookstore-web:1.0.0 web/bookstore-web
FROM node:24-alpine AS build
WORKDIR /src
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npx ng build --configuration production

FROM nginxinc/nginx-unprivileged:1.30-alpine
COPY deploy/default.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /src/dist/bookstore-web/browser /usr/share/nginx/html
# API_UPSTREAM is filled in by the chart, e.g. http://bookstore-api:8080
ENV API_UPSTREAM=http://api:8080 \
    NGINX_ENTRYPOINT_LOCAL_RESOLVERS=1
EXPOSE 8080
