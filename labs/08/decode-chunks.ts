import { StringDecoder } from "node:string_decoder";

// Bytes arrive in chunks; a chunk boundary can split a multi-byte character
const bytes = Buffer.from("Çalıkuşu;Reşat Nuri Güntekin", "utf8");
const chunks = [bytes.subarray(0, 5), bytes.subarray(5, 9), bytes.subarray(9)];

let naive = "";
for (const chunk of chunks) {
    naive += chunk.toString("utf8");
}
console.log(`naive:       ${naive}`);

const decoder = new TextDecoder("utf-8");
let streamed = "";
for (const chunk of chunks) {
    streamed += decoder.decode(chunk, { stream: true });
}
streamed += decoder.decode();
console.log(`TextDecoder: ${streamed}`);

const sd = new StringDecoder("utf8");
console.log(`StringDecoder: ${chunks.map((c) => sd.write(c)).join("") + sd.end()}`);

console.log(`base64: ${Buffer.from("Çalıkuşu").toString("base64")}`);
