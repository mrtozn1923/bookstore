CREATE ROLE replicator WITH REPLICATION LOGIN PASSWORD :'replicator_password';

CREATE TABLE IF NOT EXISTS order_events (
    event_id   bigserial PRIMARY KEY,
    order_id   bigint NOT NULL,
    kind       text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);
