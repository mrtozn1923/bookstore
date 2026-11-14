// keepalive requests share a 64 KiB budget for request bodies that are still in flight
export async function keepaliveLimit() {
  for (const size of [1000, 70000]) {
    const body = "x".repeat(size);
    const beacon = navigator.sendBeacon("/api/events", body);
    console.log(`sendBeacon(${size} bytes) -> ${beacon}`);
    try {
      await fetch("/api/events", { method: "POST", body, keepalive: true });
      console.log(`fetch keepalive (${size} bytes) -> sent`);
    } catch (err) {
      console.log(`fetch keepalive (${size} bytes) -> ${err.name}: ${err.message}`);
    }
  }
}

// Sends how long the user read the book page, once, when the page goes away
export function trackReading(isbn) {
  const started = Date.now();
  let pending = null;
  const payload = () => JSON.stringify({ type: "reading", isbn, seconds: Math.round((Date.now() - started) / 1000) });

  if ("fetchLater" in window) {
    // register now, replace it whenever the data changes; the browser sends the last one
    const update = () => {
      pending?.abort();
      pending = new AbortController();
      fetchLater("/api/events", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: payload(),
        signal: pending.signal,
      });
    };
    update();
    setInterval(update, 1000);
    return "fetchLater";
  }

  // fallback: send when the page is hidden (pagehide/unload are not reliable on mobile)
  document.addEventListener("visibilitychange", () => {
    if (document.visibilityState === "hidden") {
      fetch("/api/events", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: payload(),
        keepalive: true,
      });
    }
  });
  return "keepalive";
}
