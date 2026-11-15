import { describe, type OrderStatus } from "./order-status.ts";

// The JSON written by System.Text.Json in the C# example
const json =
  '[{"kind":"pending"},{"kind":"paid","paymentId":"PAY-1001"},{"kind":"shipped","paymentId":"PAY-1001","trackingNo":"TRK-778"},{"kind":"cancelled","reason":"customer request"}]';

const statuses = JSON.parse(json) as OrderStatus[];
for (const status of statuses) {
  console.log(describe(status));
}

const shipped = statuses[2];
if (shipped.kind === "shipped") {
  console.log("narrowed:", shipped.trackingNo);
}
