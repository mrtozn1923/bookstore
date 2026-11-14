import { EventEmitter } from "node:events";

const payments = new EventEmitter();
payments.on("captured", (orderId: string) => console.log(`captured ${orderId}`));

payments.emit("captured", "ORD-1001");
// No 'error' listener: the emitter throws the error itself
payments.emit("error", new Error("card declined for ORD-1002"));
console.log("this line never runs");
