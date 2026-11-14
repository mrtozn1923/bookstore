import { EventEmitter } from "node:events";

const orders = new EventEmitter();

// A bug: every request subscribes again and never unsubscribes
function handleRequest(requestNo: number): void {
    orders.on("orderPlaced", (id: string) => console.log(`request ${requestNo} saw ${id}`));
}

for (let i = 1; i <= 11; i++) {
    handleRequest(i);
}
console.log(`listeners: ${orders.listenerCount("orderPlaced")}`);
