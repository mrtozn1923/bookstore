import { EventEmitter, once, on } from "node:events";
import { setTimeout as delay } from "node:timers/promises";

const shipments = new EventEmitter();

// events.once: wait for a single event as a promise
setTimeout(() => shipments.emit("labelPrinted", "SHP-77", "standard"), 20);
const [shipmentId, carrier] = await once(shipments, "labelPrinted");
console.log(`once -> ${shipmentId} via ${carrier}`);

// events.on: consume events with for await; 'closed' ends the loop
(async () => {
    for (const status of ["InTransit", "OutForDelivery", "Delivered"]) {
        await delay(10);
        shipments.emit("status", "SHP-77", status);
    }
    shipments.emit("closed");
})();
for await (const [id, status] of on(shipments, "status", { close: ["closed"] })) {
    console.log(`on -> ${id} ${status}`);
}
console.log(`listeners left: ${shipments.listenerCount("status")}`);
