// A timer due in 1 ms, while a callback keeps rescheduling itself 100,000 times
function run(label: string, schedule: (fn: () => void) => void): Promise<void> {
    return new Promise((resolve) => {
        const started = performance.now();
        let calls = 0;
        let timerAfterCalls = -1;
        setTimeout(() => {
            timerAfterCalls = calls;
        }, 1);
        const step = () => {
            if (++calls < 100_000) {
                return schedule(step);
            }
            setTimeout(() => {
                console.log(`${label.padEnd(13)} done in ${Math.round(performance.now() - started)} ms, the 1 ms timer ran after ${timerAfterCalls.toLocaleString("en-US")} calls`);
                resolve();
            }, 5);
        };
        schedule(step);
    });
}

await run("nextTick", (fn) => process.nextTick(fn));
await run("setImmediate", (fn) => setImmediate(fn));
