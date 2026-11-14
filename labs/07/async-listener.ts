import { EventEmitter } from "node:events";

// An async listener that fails: who catches the rejection?
const orders = new EventEmitter({ captureRejections: process.argv[2] === "capture" });
orders.on("orderPlaced", async (id: string) => {
    await Promise.resolve();
    throw new Error(`loyalty service unavailable for ${id}`);
});
orders.on("error", (err: Error) => console.log(`'error' listener: ${err.message}`));

orders.emit("orderPlaced", "ORD-1001");
setTimeout(() => console.log("process is still alive"), 10);
