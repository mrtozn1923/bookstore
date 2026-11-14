# PostgreSQL 18 image + PostgreSQL 14 binaries, for the public schema check of post 15
FROM postgres:18
RUN sed -i 's/ main 18$/ main 14 18/' /etc/apt/sources.list.d/pgdg.list \
    && apt-get update && apt-get install -y --no-install-recommends postgresql-14 && rm -rf /var/lib/apt/lists/*
