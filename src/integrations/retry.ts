import { setTimeout as delay } from "node:timers/promises";

export interface RetryOptions {
    // Total number of attempts, the first one included
    attempts: number;
    baseDelayMs: number;
    maxDelayMs: number;
    // Decides per error whether another attempt makes sense
    isRetryable: (err: unknown) => boolean;
    // The caller's budget: no new attempt and no waiting after it is aborted
    signal?: AbortSignal;
    onRetry?: (info: { attempt: number; delayMs: number; err: unknown }) => void;
    random?: () => number;
}

// An error may carry the server's Retry-After hint
export interface RetryAfterHint {
    retryAfterMs?: number;
}

// Exponential backoff with "full jitter": a random wait between 0 and base * 2^(attempt-1), capped
export function backoffDelay(attempt: number, baseDelayMs: number, maxDelayMs: number, random: () => number = Math.random): number {
    const ceiling = Math.min(maxDelayMs, baseDelayMs * 2 ** (attempt - 1));
    return Math.round(random() * ceiling);
}

export async function withRetry<T>(operation: (attempt: number) => Promise<T>, options: RetryOptions): Promise<T> {
    for (let attempt = 1; ; attempt++) {
        options.signal?.throwIfAborted();
        try {
            return await operation(attempt);
        } catch (err) {
            if (attempt >= options.attempts || options.signal?.aborted || !options.isRetryable(err)) {
                throw err;
            }
            const hinted = (err as RetryAfterHint).retryAfterMs;
            const delayMs = Math.min(options.maxDelayMs, hinted ?? backoffDelay(attempt, options.baseDelayMs, options.maxDelayMs, options.random));
            options.onRetry?.({ attempt, delayMs, err });
            try {
                await delay(delayMs, undefined, { signal: options.signal });
            } catch (waitError) {
                // timers/promises rejects with its own AbortError; surface the caller's reason instead
                options.signal?.throwIfAborted();
                throw waitError;
            }
        }
    }
}
