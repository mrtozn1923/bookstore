// Runs the price report in each mode and measures what other requests experience meanwhile
const base = process.env.API_URL ?? "http://127.0.0.1:3000";

async function getJson<T>(path: string): Promise<T> {
    const res = await fetch(base + path);
    return (await res.json()) as T;
}

for (const mode of ["inline", "chunked", "worker"]) {
    await getJson("/diagnostics/event-loop");
    // Right after a reset the first sample can be lost; give the monitor a few ticks
    await new Promise((r) => setTimeout(r, 50));
    let done = false;
    const report = getJson<{ durationMs: number; revenue: number }>(`/api/reports/prices?mode=${mode}`).finally(() => (done = true));

    // A health probe every 20 ms while the report is running
    let slowest = 0;
    while (!done) {
        const started = performance.now();
        await getJson("/health");
        slowest = Math.max(slowest, performance.now() - started);
        await new Promise((r) => setTimeout(r, 20));
    }
    const { durationMs, revenue } = await report;
    const loop = await getJson<{ p99Ms: number; maxMs: number }>("/diagnostics/event-loop");
    console.log(
        `${mode.padEnd(8)} report ${String(durationMs).padStart(5)} ms | slowest /health ${slowest.toFixed(0).padStart(5)} ms | loop delay p99 ${loop.p99Ms} ms, max ${loop.maxMs} ms | revenue ${revenue}`,
    );
}
