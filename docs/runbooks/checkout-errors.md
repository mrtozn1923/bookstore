# CheckoutErrorBudgetBurn

**What it means:** more than 7.2% of `POST /api/orders` requests end with 5xx, both over the last hour and the last
5 minutes. At this rate the 30-day error budget (0.5%) is gone in about two days.

**Who is affected:** customers who cannot place an order. Every minute counts.

**First 5 minutes**
1. Open the "Bookstore API (RED)" dashboard: which status codes, since when, one instance or all?
2. Look at "Payment provider: retries and timeouts". 502s with many timeouts point at the payment provider.
3. In Grafana Explore (Loki): `{service_name="bookstore-api"} | detected_level="error"`; open a line and follow its `trace_id` to Tempo.
4. Was there a deploy in the last hour? If yes and the errors started with it: roll back first, investigate later.

**If it is the payment provider:** check the provider's status page, tell customer service (Gül) what customers see,
and open an incident with the provider. Do not raise retries; it adds load to a provider that is already failing.

**Close the loop:** write down what happened and whether this alert came early enough.
