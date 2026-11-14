// the same pattern in V8: no timeout option exists, the call blocks the thread until it finishes
const backtracking = /^(a+)+$/;
console.log("n".padStart(3), "backtracking ms".padStart(16));
for (let n = 16; n <= 28; n += 2) {
  const input = "a".repeat(n) + "!";
  const start = performance.now();
  backtracking.test(input);
  console.log(String(n).padStart(3), (performance.now() - start).toFixed(1).padStart(16));
}
