import { readFileSync } from "node:fs";

const text = readFileSync("order.json", "utf8");

// the reviver's third argument carries the original source text (ES2026 JSON.parse source text access)
const order = JSON.parse(text, (key, value, context) =>
  key === "orderId" ? BigInt(context.source) : value);

console.log(order.orderId, typeof order.orderId);
console.log(JSON.stringify({ orderId: order.orderId.toString() }));
