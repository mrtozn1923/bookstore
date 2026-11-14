// Order 1042's status endpoint for virtual-time polling examples.
// Each call answers the next status; latencies[i] is how long call i takes (default 300 ms).
import { fakeRequest } from "../04/fake-api.ts";

export type Status = "Paid" | "Shipped" | "Delivered";

export function statusApi(latencies: number[] = []) {
  const statuses: Status[] = ["Paid", "Paid", "Shipped", "Shipped", "Delivered"];
  let calls = 0;
  return () => {
    const i = Math.min(calls++, statuses.length - 1);
    return fakeRequest(`GET /orders/1042/status #${i + 1}`, latencies[i] ?? 300, () => statuses[i]);
  };
}
