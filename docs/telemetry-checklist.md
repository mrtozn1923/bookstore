# Telemetry checklist before a service goes to production

- **Resource:** `service.name`, `service.version` and `deployment.environment.name` are correct and the same on every signal.
- **Traces:** incoming and outgoing HTTP, database and message queue calls produce spans; context propagation does not break between services.
- **Metrics:** RED metrics exist, business metrics are defined, the cardinality of every label has been estimated.
- **Logs:** levels are set for production, log records carry `trace_id`, no personal data (or it is masked in the Collector).
- **Sampling:** tail sampling keeps errors and slow requests; no head sampling in the application.
- **Dashboards:** the service's RED dashboard is in Git and loaded by provisioning.
- **Alerts:** symptom alerts, a "no telemetry" alert and a runbook for each; the notification channel has been tested.
- **Cost:** daily spans, log records and series are estimated; retention periods are decided.
