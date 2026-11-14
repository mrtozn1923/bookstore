FROM node:24-alpine AS build
WORKDIR /src
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npx ng build

FROM nginx:1.30-alpine
COPY docker/default.conf /etc/nginx/conf.d/default.conf
COPY docker/config.template.json /etc/bookstore/config.template.json
COPY docker/40-runtime-config.sh /docker-entrypoint.d/40-runtime-config.sh
COPY --from=build /src/dist/bookstore-web/browser /usr/share/nginx/html
# Image'daki geliştirme config'i silinir; config.json yalnız runtime'da üretilir
RUN rm /usr/share/nginx/html/config.json
