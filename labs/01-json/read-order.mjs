import { readFileSync } from "node:fs";

const order = JSON.parse(readFileSync("order.json", "utf8"));
console.log(order.orderId, order.status, order.total);
console.log(JSON.stringify(order));
