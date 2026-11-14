# PaymentProviderRetriesHigh

**What it means:** more than 20% of payment calls needed a retry for 15 minutes. Customers may not notice yet,
but checkout gets slower and the provider gets extra load. This is a ticket, not a page.

**What to do (working hours)**
1. Tempo: `{ name = "authorize payment" } > { span.http.request.method = "POST" } | count() > 1` shows which calls retried and why.
2. Compare attempt durations with the attempt timeout in `Payments/PaymentsModule.cs` (4 s). Timeouts below the
   provider's normal answers turn into a retry storm.
3. Contact the provider if their latency changed.
