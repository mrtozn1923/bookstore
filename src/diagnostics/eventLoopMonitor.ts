import { monitorEventLoopDelay } from "node:perf_hooks";

// Samples event loop delay every 10 ms; values are in nanoseconds
const histogram = monitorEventLoopDelay({ resolution: 10 });
histogram.enable();

const toMs = (ns: number): number => Math.round(ns / 1e5) / 10;

// Returns the delay since the last call and starts a new measurement window
export function takeEventLoopDelay(): { p50Ms: number; p99Ms: number; maxMs: number } {
    const stats = { p50Ms: toMs(histogram.percentile(50)), p99Ms: toMs(histogram.percentile(99)), maxMs: toMs(histogram.max) };
    histogram.reset();
    return stats;
}
