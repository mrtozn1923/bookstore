# PostgreSQL 18 + pg_cron from the PGDG apt repository that the official image already uses
FROM postgres:18
RUN apt-get update \
    && apt-get install -y --no-install-recommends postgresql-18-cron \
    && rm -rf /var/lib/apt/lists/*
