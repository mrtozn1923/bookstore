// unload vs. pagehide: which handler still runs when the tab closes?
export function reportWhenLeaving() {
  addEventListener("unload", () => navigator.sendBeacon("/api/events", "unload fired"));
  addEventListener("pagehide", (event) => navigator.sendBeacon("/api/events", `pagehide fired, persisted=${event.persisted}`));
  addEventListener("visibilitychange", () => {
    if (document.visibilityState === "hidden") navigator.sendBeacon("/api/events", "visibilitychange hidden");
  });
}
