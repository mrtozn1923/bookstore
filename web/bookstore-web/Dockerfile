# Build context: web/bookstore-web
FROM node:24-alpine AS build
WORKDIR /src
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npx ng build --configuration production

FROM nginxinc/nginx-unprivileged:1.30-alpine
# The template is rendered by the image's entrypoint (envsubst) into /etc/nginx/conf.d/default.conf.
ENV NGINX_ENTRYPOINT_LOCAL_RESOLVERS=1
COPY nginx/default.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /src/dist/bookstore-web/browser /usr/share/nginx/html
EXPOSE 8080
