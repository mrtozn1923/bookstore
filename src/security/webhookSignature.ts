import { createHmac, timingSafeEqual } from "node:crypto";

// Header sent by the payment provider: "t=<unix seconds>,sig=<hex HMAC-SHA256 of `${t}.${raw body}`>"
export const SIGNATURE_HEADER = "x-payment-signature";
const TOLERANCE_SECONDS = 300;

export type SignatureCheck = { ok: true } | { ok: false; reason: "missing" | "malformed" | "expired" | "mismatch" };

export function signPayload(rawBody: Buffer, secret: string, timestamp: number): string {
    const sig = createHmac("sha256", secret).update(`${timestamp}.`).update(rawBody).digest("hex");
    return `t=${timestamp},sig=${sig}`;
}

// Verifies the signature over the exact bytes that arrived, never over re-serialised JSON
export function verifyWebhookSignature(rawBody: Buffer, header: string | undefined, secret: string, nowSeconds = Math.floor(Date.now() / 1000)): SignatureCheck {
    if (!header) {
        return { ok: false, reason: "missing" };
    }
    const match = /^t=(\d{1,12}),sig=([0-9a-f]{64})$/.exec(header);
    if (!match) {
        return { ok: false, reason: "malformed" };
    }
    const timestamp = Number(match[1]);
    // An old but valid signature is a replayed request: accept only a few minutes of clock difference
    if (Math.abs(nowSeconds - timestamp) > TOLERANCE_SECONDS) {
        return { ok: false, reason: "expired" };
    }
    const expected = Buffer.from(signPayload(rawBody, secret, timestamp).split("sig=")[1], "hex");
    const received = Buffer.from(match[2], "hex");
    // Constant time: the comparison takes as long for the first wrong byte as for the last one
    return timingSafeEqual(expected, received) ? { ok: true } : { ok: false, reason: "mismatch" };
}
