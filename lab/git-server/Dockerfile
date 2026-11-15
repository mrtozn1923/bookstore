# Read-only Git server over smart HTTP (git http-backend behind nginx + fcgiwrap). Lab use only.
FROM alpine:3.22
RUN apk add --no-cache git git-daemon nginx fcgiwrap spawn-fcgi \
    && git config --system --add safe.directory '*'
COPY nginx.conf /etc/nginx/http.d/default.conf
COPY start.sh /start.sh
EXPOSE 8080
CMD ["/bin/sh", "/start.sh"]
