# BookstoreApiTelemetryMissing

**What it means:** Prometheus has received no `target_info` from `bookstore-api` for 7 minutes. Either the API is down,
or the telemetry pipeline (Collector, network, Prometheus) is. While this fires, the API's other alerts are inhibited.

**First 5 minutes**
1. Does the API answer? `curl -i https://<api-host>/api/books?pageSize=1`
2. If it answers: the pipeline is broken. Check the Collector's logs and its own metrics (`otelcol_*`).
3. If it does not: the deployment and the host. Customers cannot order at all; treat as the highest priority.
