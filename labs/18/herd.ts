import http from "node:http";
import { withRetry } from "../../src/integrations/retry.ts";

// 100 callers hit a carrier that is down for the first 1.5 s; how do their retries arrive?
async function run(label: string, random: () => number): Promise<void> {
    const arrivals: number[] = [];
    const started = performance.now();
    const carrier = http.createServer((req, res) => {
        const at = performance.now() - started;
        arrivals.push(at);
        res.writeHead(at < 1_500 ? 503 : 200).end();
    });
    await new Promise<void>((resolve) => carrier.listen(52185, "127.0.0.1", resolve));

    await Promise.all(
        Array.from({ length: 100 }, () =>
            withRetry(
                async () => {
                    const response = await fetch("http://127.0.0.1:52185/v1/tracking/BK1001TR");
                    await response.body?.cancel();
                    if (!response.ok) {
                        throw new Error(String(response.status));
                    }
                },
                { attempts: 8, baseDelayMs: 200, maxDelayMs: 5_000, isRetryable: () => true, random },
            ),
        ),
    );
    // Requests per 250 ms slot: a wave shows up as one big number
    const slots = new Array<number>(Math.ceil(Math.max(...arrivals) / 250)).fill(0);
    arrivals.forEach((at) => slots[Math.floor(at / 250)]++);
    console.log(`${label}: ${arrivals.length} requests, done at ${Math.round(performance.now() - started)} ms`);
    console.log(`  per 250 ms: ${slots.join(" ")}`);
    carrier.closeAllConnections();
    carrier.close();
}

await run("no jitter  ", () => 1);
await run("full jitter", Math.random);
