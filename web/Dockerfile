# syntax=docker/dockerfile:1

# build: Node runs on the build machine's platform; the output is plain JavaScript
FROM --platform=$BUILDPLATFORM node:26-alpine AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN --mount=type=cache,id=npm,target=/root/.npm \
    npm ci
COPY . .
RUN npm run build

# dev: ng serve for Compose Watch (docker compose --profile dev watch); not part of the final image
FROM node:26-alpine AS dev
ENV NG_CLI_ANALYTICS=false
WORKDIR /app
RUN chown node:node /app
USER node
COPY --chown=node:node package.json package-lock.json ./
RUN npm ci
COPY --chown=node:node . .
EXPOSE 4200
CMD ["npx", "ng", "serve", "--host", "0.0.0.0", "--port", "4200"]

# final: unprivileged nginx (uid 101, port 8080) serving static files
FROM nginxinc/nginx-unprivileged:1.30-alpine AS final
COPY nginx/default.conf /etc/nginx/conf.d/default.conf
COPY nginx/config.template.json /etc/bookstore/config.template.json
COPY --chmod=755 nginx/40-runtime-config.sh /docker-entrypoint.d/40-runtime-config.sh
COPY --from=build /app/dist/bookstore-web/browser /usr/share/nginx/html
EXPOSE 8080
