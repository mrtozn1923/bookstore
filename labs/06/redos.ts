// Each extra character roughly doubles the work of the vulnerable pattern
const vulnerable = /^([A-Z0-9]+-?)+$/;
const safe = /^[A-Z0-9]+(-[A-Z0-9]+)*$/;

// Warm-up: V8 compiles a regular expression to machine code after a few runs
for (let i = 0; i < 20; i++) {
    vulnerable.test("SUMMER-2026!");
    safe.test("SUMMER-2026!");
}

for (const length of [20, 22, 24, 26, 28]) {
    const input = "BOOKSTORE2026BOOKSTORE2026BOOK".slice(0, length - 1) + "!";
    let started = performance.now();
    vulnerable.test(input);
    const slow = performance.now() - started;
    started = performance.now();
    safe.test(input);
    const fast = performance.now() - started;
    console.log(`${String(length).padStart(2)} chars: vulnerable ${slow.toFixed(1).padStart(7)} ms, safe ${fast.toFixed(3)} ms`);
}
console.log(safe.test("SUMMER-2026-BOOKS"), safe.test("SUMMER--2026"));
