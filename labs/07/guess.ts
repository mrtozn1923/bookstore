import { EventEmitter } from "node:events";

const orders = new EventEmitter();
orders.on("orderPlaced", (id: string) => console.log(`send confirmation e-mail for ${id}`));
orders.on("orderPlaced", async (id: string) => {
    await Promise.resolve();
    console.log(`update sales dashboard for ${id}`);
});

console.log("before emit");
const handled = orders.emit("orderPlaced", "ORD-1001");
console.log(`after emit, handled = ${handled}`);
