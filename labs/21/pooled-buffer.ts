import { MessageChannel } from "node:worker_threads";

// A small Buffer is cut from Node's shared buffer pool; its ArrayBuffer belongs to other Buffers too
const isbn = Buffer.from("9780441172719");
console.log(`byteOffset ${isbn.byteOffset}, buffer ${isbn.buffer.byteLength} bytes for ${isbn.length} bytes of data`);
const { port1, port2 } = new MessageChannel();
try {
    port1.postMessage(isbn, [isbn.buffer as ArrayBuffer]);
} catch (err) {
    console.log(`${(err as Error).name}: ${(err as Error).message}`);
}
port1.close();
port2.close();
