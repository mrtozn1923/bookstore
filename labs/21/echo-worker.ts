// Replies with the size of what it received
import { parentPort } from "node:worker_threads";

parentPort!.on("message", (bytes: ArrayBuffer) => parentPort!.postMessage(bytes.byteLength));
