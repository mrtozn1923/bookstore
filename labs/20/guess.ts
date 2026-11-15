import { createHmac } from "node:crypto";

const secret = "test-webhook-secret-not-real";

// The payment provider signs exactly the bytes it sends (spaces included)
const rawBody = '{"id": "pay-evt-1001", "type": "payment.succeeded", "orderId": "1001", "amountMinor": 34990, "currency": "TRY"}';
const sent = createHmac("sha256", secret).update(rawBody).digest("hex");

// Our handler gets req.body from express.json() and signs it again
const parsed = JSON.parse(rawBody);
const ours = createHmac("sha256", secret).update(JSON.stringify(parsed)).digest("hex");

console.log(sent === ours ? "signature OK" : "signature mismatch");
