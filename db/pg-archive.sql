CREATE TABLE orders_archive (LIKE orders);
CREATE TABLE order_items_archive (LIKE order_items);

CREATE TABLE archive_progress (
    job_name      text PRIMARY KEY,
    last_order_id bigint NOT NULL,
    moved_orders  bigint NOT NULL,
    updated_at    timestamptz NOT NULL
);
