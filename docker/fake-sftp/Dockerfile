# A fake supplier SFTP server for local tests: OpenSSH, key-only login, chrooted user "supplier".
FROM alpine:3.22
RUN apk add --no-cache openssh-server \
    && adduser -D -u 1001 supplier \
    && sed -i 's/^supplier:!/supplier:*/' /etc/shadow \
    && mkdir -p /srv/sftp/supplier/outbound/processed /srv/sftp/supplier/inbound \
    && chown -R supplier:supplier /srv/sftp/supplier/outbound /srv/sftp/supplier/inbound
COPY sshd_config /etc/ssh/sshd_config
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh
EXPOSE 22
ENTRYPOINT ["/entrypoint.sh"]
