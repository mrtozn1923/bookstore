import { randomUUID } from "node:crypto";
import { Agent, type Dispatcher } from "undici";
import { z } from "zod";
import { withRetry, type RetryAfterHint } from "./retry.ts";

const trackingSchema = z.object({
    trackingNumber: z.string(),
    status: z.enum(["label_created", "in_transit", "out_for_delivery", "delivered", "returned"]),
    events: z.array(z.object({ at: z.string(), status: z.string(), location: z.string() })),
});
export type Tracking = z.infer<typeof trackingSchema>;

const shipmentSchema = z.object({ trackingNumber: z.string(), orderId: z.string() });
export type Shipment = z.infer<typeof shipmentSchema>;

export class TrackingNotFoundError extends Error {
    constructor(trackingNumber: string) {
        super(`tracking number ${trackingNumber} is unknown to the carrier`);
        this.name = "TrackingNotFoundError";
    }
}

// The carrier answered with an error status or could not be reached in time
export class CarrierUnavailableError extends Error implements RetryAfterHint {
    readonly status?: number;
    readonly retryAfterMs?: number;
    readonly timedOut: boolean;

    constructor(message: string, options: { status?: number; retryAfterMs?: number; timedOut?: boolean; cause?: unknown } = {}) {
        super(message, { cause: options.cause });
        this.name = "CarrierUnavailableError";
        this.status = options.status;
        this.retryAfterMs = options.retryAfterMs;
        this.timedOut = options.timedOut ?? false;
    }
}

export interface CarrierClientOptions {
    baseUrl: string;
    apiKey: string;
    // Per attempt: connecting, headers and body together
    timeoutMs: number;
    attempts: number;
    dispatcher?: Dispatcher;
    onRetry?: (info: { attempt: number; delayMs: number; err: unknown }) => void;
}

const RETRYABLE_STATUS = new Set([408, 429, 500, 502, 503, 504]);

// Retry-After is either seconds or an HTTP date (RFC 9110)
function parseRetryAfter(value: string | null): number | undefined {
    if (!value) {
        return undefined;
    }
    const seconds = Number(value);
    const ms = Number.isFinite(seconds) ? seconds * 1000 : Date.parse(value) - Date.now();
    return Number.isFinite(ms) ? Math.max(0, ms) : undefined;
}

// One keep-alive connection pool per process for the carrier (IHttpClientFactory's pooled handler counterpart)
export function createCarrierAgent(): Agent {
    return new Agent({ connections: 20, keepAliveTimeout: 10_000, connectTimeout: 2_000 });
}

export class CarrierClient {
    readonly #options: CarrierClientOptions;

    constructor(options: CarrierClientOptions) {
        this.#options = options;
    }

    // GET is idempotent: safe to retry on timeouts, network errors and 5xx/429
    async getTracking(trackingNumber: string, signal?: AbortSignal): Promise<Tracking> {
        const url = new URL(`/v1/tracking/${encodeURIComponent(trackingNumber)}`, this.#options.baseUrl);
        return this.#withRetry(signal, async () => {
            const response = await this.#send(url, { method: "GET" }, signal);
            if (response.status === 404) {
                await response.body?.cancel();
                throw new TrackingNotFoundError(trackingNumber);
            }
            return trackingSchema.parse(await response.json());
        });
    }

    // POST is not idempotent by itself: one key per logical operation makes the retries safe
    async createShipment(orderId: string, signal?: AbortSignal): Promise<Shipment> {
        const url = new URL("/v1/shipments", this.#options.baseUrl);
        const idempotencyKey = randomUUID();
        return this.#withRetry(signal, async () => {
            const response = await this.#send(
                url,
                {
                    method: "POST",
                    headers: { "content-type": "application/json", "idempotency-key": idempotencyKey },
                    body: JSON.stringify({ orderId }),
                },
                signal,
            );
            return shipmentSchema.parse(await response.json());
        });
    }

    #withRetry<T>(signal: AbortSignal | undefined, operation: () => Promise<T>): Promise<T> {
        return withRetry(operation, {
            attempts: this.#options.attempts,
            baseDelayMs: 200,
            maxDelayMs: 2_000,
            signal,
            isRetryable: (err) => err instanceof CarrierUnavailableError,
            onRetry: this.#options.onRetry,
        });
    }

    // One attempt: own timeout, linked to the caller's signal; only 2xx and 404 come back
    async #send(url: URL, init: RequestInit, signal?: AbortSignal): Promise<Response> {
        const timeout = AbortSignal.timeout(this.#options.timeoutMs);
        const combined = signal ? AbortSignal.any([signal, timeout]) : timeout;
        let response: Response;
        try {
            response = await fetch(url, {
                ...init,
                headers: { accept: "application/json", "x-api-key": this.#options.apiKey, ...init.headers },
                signal: combined,
                // A Node.js extension to fetch: which undici dispatcher (connection pool) to use
                ...(this.#options.dispatcher ? { dispatcher: this.#options.dispatcher } : {}),
            } as RequestInit);
        } catch (err) {
            if (signal?.aborted) {
                // The caller gave up (client disconnected): not the carrier's fault, do not retry
                throw err;
            }
            const timedOut = timeout.aborted;
            throw new CarrierUnavailableError(timedOut ? `carrier did not answer within ${this.#options.timeoutMs} ms` : "carrier unreachable", {
                timedOut,
                cause: err,
            });
        }
        if (response.ok || response.status === 404) {
            return response;
        }
        // Read (or cancel) every body, otherwise the connection stays busy until garbage collection
        await response.body?.cancel();
        if (RETRYABLE_STATUS.has(response.status)) {
            throw new CarrierUnavailableError(`carrier answered ${response.status}`, {
                status: response.status,
                retryAfterMs: parseRetryAfter(response.headers.get("retry-after")),
            });
        }
        throw new Error(`carrier rejected the request with ${response.status}`);
    }
}
