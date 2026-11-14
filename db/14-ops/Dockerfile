# PostgreSQL 18 image + PostgreSQL 17 binaries (and pg_cron for 17) for the pg_upgrade and PITR demos of post 14
FROM postgres:18
RUN sed -i 's/ main 18$/ main 17 18/' /etc/apt/sources.list.d/pgdg.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends postgresql-17 postgresql-17-cron \
    && rm -rf /var/lib/apt/lists/*
