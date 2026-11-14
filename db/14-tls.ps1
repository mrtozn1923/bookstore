# Post 14 - TLS for the lab server and an SSL-only rule for the application role (PowerShell, repo root).
# 1. self-signed certificate for "localhost" in the data directory, then turn ssl on (a reload is enough)
docker exec -u postgres bookstore-pg bash -c "cd /var/lib/postgresql/18/docker && openssl req -new -x509 -days 365 -nodes -subj /CN=localhost -addext subjectAltName=DNS:localhost -keyout server.key -out server.crt && chmod 600 server.key"
docker exec bookstore-pg psql -U postgres -c "ALTER SYSTEM SET ssl = on" -c "SELECT pg_reload_conf()"
docker cp bookstore-pg:/var/lib/postgresql/18/docker/server.crt .\server.crt

# 2. bookstore_app may connect only over TLS (rule inserted before the image's last "host all all all" line)
docker exec -u postgres bookstore-pg bash -c "cd /var/lib/postgresql/18/docker && cp pg_hba.conf pg_hba.conf.orig && sed -i 's/^host all all all scram-sha-256$/hostnossl all bookstore_app all reject\nhostssl all all all scram-sha-256\nhost all all all scram-sha-256/' pg_hba.conf"
docker exec bookstore-pg psql -U postgres -c "SELECT pg_reload_conf()" -c "SELECT line_number, type, database, user_name, address, auth_method FROM pg_hba_file_rules WHERE line_number > 126"

# 3. try every SSL Mode from .NET
$env:BOOKSTORE_PG_ROOT_CERT = "$PWD\server.crt"
dotnet run --project src/Bookstore.PgLab -- tls

# undo: copy pg_hba.conf.orig back, ALTER SYSTEM RESET ssl, reload
