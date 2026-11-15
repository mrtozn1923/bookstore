import { monitorEventLoopDelay } from "node:perf_hooks";
import { hashPassword, verifyPassword } from "../../src/security/passwords.ts";

// 16 sign-ins at the same moment: what happens to time, memory and the event loop?
const stored = await hashPassword("correct horse battery staple");
const delay = monitorEventLoopDelay({ resolution: 10 });
let peak = 0;
const sampler = setInterval(() => (peak = Math.max(peak, process.memoryUsage.rss())), 10);
delay.enable();
const started = performance.now();
const results = await Promise.all(Array.from({ length: 16 }, () => verifyPassword("correct horse battery staple", stored)));
const elapsed = performance.now() - started;
delay.disable();
clearInterval(sampler);
console.log(`16 verifications: all ${results.every(Boolean)}, ${Math.round(elapsed)} ms, peak RSS ${Math.round(peak / 1e6)} MB, max event loop delay ${Math.round(delay.max / 1e6)} ms`);
