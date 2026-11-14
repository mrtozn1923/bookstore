# CheckoutLatencyHigh

**What it means:** the p95 of checkout (`bookstore:checkout_duration_seconds:p95_5m`) has been above 3 seconds for 10 minutes.

**First 5 minutes**
1. "Bookstore API (RED)" dashboard: is it all routes or only `POST /api/orders/`? Click an exemplar on the p99 line.
2. In the trace, which span takes the time: SQL, the payment call, or our own code?
3. "Bookstore API runtime (USE)": thread pool queue and CPU. A queue that does not drain means the API itself is saturated.
4. Payment provider slow: see payment-provider.md.
