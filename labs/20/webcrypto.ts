import { createHmac } from "node:crypto";

const secret = "test-webhook-secret-not-real";
const payload = '1763190000.{"id":"pay-evt-1001","type":"payment.succeeded"}';

// node:crypto: synchronous, Node only
const viaNode = createHmac("sha256", secret).update(payload).digest("hex");

// Web Crypto (globalThis.crypto.subtle): async, the same API in browsers, Deno, Bun and edge runtimes
const encoder = new TextEncoder();
const key = await crypto.subtle.importKey("raw", encoder.encode(secret), { name: "HMAC", hash: "SHA-256" }, false, ["sign", "verify"]);
const signature = await crypto.subtle.sign("HMAC", key, encoder.encode(payload));
const viaWeb = Buffer.from(signature).toString("hex");

console.log(viaNode === viaWeb ? "same signature" : "different");
// verify() compares in constant time for you
console.log("verify:", await crypto.subtle.verify("HMAC", key, Buffer.from(viaNode, "hex"), encoder.encode(payload)));
