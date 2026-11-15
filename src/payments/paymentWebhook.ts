import express, { Router } from "express";
import { z } from "zod";
import { HttpError } from "#http/errors";
import { SIGNATURE_HEADER, verifyWebhookSignature } from "../security/webhookSignature.ts";
import { logger } from "../diagnostics/logger.ts";

const paymentEventSchema = z.object({
    id: z.string().min(1).max(100),
    type: z.enum(["payment.succeeded", "payment.failed"]),
    orderId: z.string().min(1),
    amountMinor: z.number().int().positive(),
    currency: z.enum(["TRY", "EUR"]),
});
export type PaymentEvent = z.infer<typeof paymentEventSchema>;

// What happens with a verified event; returns false for an event id it has already seen
export type PaymentEventHandler = (event: PaymentEvent) => Promise<boolean>;

// In memory for now: a production version keeps the ids in a table with a unique key
export function createInMemoryPaymentEvents(): PaymentEventHandler {
    const seen = new Set<string>();
    return async (event) => {
        if (seen.has(event.id)) {
            return false;
        }
        seen.add(event.id);
        logger.info({ eventId: event.id, type: event.type, orderId: event.orderId }, "payment event recorded");
        return true;
    };
}

// POST /api/payments/webhook: no bearer token; the HMAC signature is the authentication
export function paymentWebhookRouter(secret: string, handle: PaymentEventHandler): Router {
    // express.raw keeps the body as a Buffer: the signature covers the bytes, not the parsed object
    return Router().post("/", express.raw({ type: "application/json", limit: "64kb" }), async (req, res) => {
        const body: Buffer = Buffer.isBuffer(req.body) ? req.body : Buffer.alloc(0);
        const check = verifyWebhookSignature(body, req.get(SIGNATURE_HEADER), secret);
        if (!check.ok) {
            logger.warn({ reason: check.reason }, "webhook signature rejected");
            throw new HttpError(401, "invalid webhook signature");
        }
        let json: unknown;
        try {
            json = JSON.parse(body.toString("utf8"));
        } catch {
            throw new HttpError(400, "webhook body is not valid JSON");
        }
        const parsed = paymentEventSchema.safeParse(json);
        if (!parsed.success) {
            throw new HttpError(400, "unexpected webhook payload");
        }
        const isNew = await handle(parsed.data);
        // 200 for duplicates too: the provider must stop retrying an event we already have
        res.status(200).json({ received: true, duplicate: !isNew });
    });
}
