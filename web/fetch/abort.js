// The user's cancel button and a timeout, combined into one signal
export async function loadReport(url, userSignal, timeoutMs = 5000) {
  const signal = AbortSignal.any([userSignal, AbortSignal.timeout(timeoutMs)]);
  // the listener also keeps the combined signal alive in WebKit (see the note below)
  signal.addEventListener("abort", () => console.log(`  abort event: ${signal.reason.name}`), { once: true });
  try {
    const response = await fetch(url, { signal });
    return await response.text(); // the signal also cancels reading the body
  } catch (err) {
    // WebKit rejects with AbortError even after a timeout; the signal's reason is reliable
    throw signal.aborted ? signal.reason : err;
  }
}

export async function abortScenarios() {
  const cases = [
    ["slow server, 1 s timeout", "/api/reports/orders.csv?rows=10&delay=3000", 1000, null],
    ["user cancels after 300 ms", "/api/reports/orders.csv?rows=10&delay=3000", 5000, 300],
    ["cancel while the body streams", "/api/reports/orders.csv?rows=50000", 5000, 300],
    ["fast server", "/api/reports/orders.csv?rows=10", 1000, null],
  ];
  for (const [name, url, timeoutMs, cancelAfterMs] of cases) {
    const controller = new AbortController();
    if (cancelAfterMs) setTimeout(() => controller.abort(), cancelAfterMs);
    const started = performance.now();
    try {
      const csv = await loadReport(url, controller.signal, timeoutMs);
      console.log(`${name}: ok, ${csv.length} chars`);
    } catch (err) {
      const ms = (performance.now() - started).toFixed(0);
      console.log(`${name}: ${err.name} after ${ms} ms`);
    }
  }
}
