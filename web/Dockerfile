# syntax=docker/dockerfile:1

# build: Node runs on the build machine's platform; the output is plain JavaScript
FROM --platform=$BUILDPLATFORM node:26-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN --mount=type=cache,id=npm,target=/root/.npm \
    npm ci
COPY . .
RUN npm run build

# final: unprivileged nginx (uid 101, port 8080) serving static files
FROM nginxinc/nginx-unprivileged:1.30-alpine AS final
COPY nginx/default.conf /etc/nginx/conf.d/default.conf
COPY nginx/config.template.json /etc/bookstore/config.template.json
COPY --chmod=755 nginx/40-runtime-config.sh /docker-entrypoint.d/40-runtime-config.sh
COPY --from=build /app/dist/bookstore-web/browser /usr/share/nginx/html
# config.json is rewritten at startup, so the nginx user must own it
COPY --from=build --chown=101:101 /app/dist/bookstore-web/browser/config.json /usr/share/nginx/html/config.json
EXPOSE 8080
