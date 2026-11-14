export const VERSION = "v1";

const log = (text) => {
  console.log(text);
  document.querySelector("#log").textContent += `${text}\n`;
};

// messages from the service worker
navigator.serviceWorker.addEventListener("message", (event) => log(`[sw ${event.data.version}] ${event.data.text}`));
navigator.serviceWorker.addEventListener("controllerchange", () => log("controllerchange: a new service worker controls this page"));

// asks a worker which version it is
function ask(worker) {
  return new Promise((resolve) => {
    if (!worker) return resolve("-");
    const channel = new MessageChannel();
    channel.port1.onmessage = (event) => resolve(event.data);
    worker.postMessage("version", [channel.port2]);
  });
}

async function track(worker) {
  const version = await ask(worker);
  log(`worker ${version}: ${worker.state}`);
  worker.addEventListener("statechange", () => log(`worker ${version}: ${worker.state}`));
}

// updatefound fires for the first install and for every new version found later
const watched = new WeakSet();
function watch(registration) {
  if (watched.has(registration)) return;
  watched.add(registration);
  registration.addEventListener("updatefound", () => track(registration.installing));
}
navigator.serviceWorker.getRegistration("./").then((registration) => registration && watch(registration));

export async function register(options = {}) {
  const registration = await navigator.serviceWorker.register("./sw.js", { scope: "./", ...options });
  log(`registered, scope ${new URL(registration.scope).pathname}`);
  watch(registration);
}

// which version controls the page, and what is waiting
export async function status() {
  const registration = await navigator.serviceWorker.getRegistration("./");
  const controller = await ask(navigator.serviceWorker.controller);
  const waiting = await ask(registration?.waiting);
  const active = await ask(registration?.active);
  const cacheNames = (await caches.keys()).join(", ");
  const text = `page ${VERSION}, controller ${controller}, active ${active}, waiting ${waiting}; caches: ${cacheNames}`;
  log(text);
  return text;
}

export async function update() {
  const registration = await navigator.serviceWorker.getRegistration("./");
  await registration.update();
  await new Promise((resolve) => setTimeout(resolve, 500));
  return status();
}

// "Reload to update" button: tell the waiting worker to take over, then reload once it does
export async function activateWaiting({ reload }) {
  const registration = await navigator.serviceWorker.getRegistration("./");
  if (!registration?.waiting) return "nothing waiting";
  if (reload) navigator.serviceWorker.addEventListener("controllerchange", () => location.reload(), { once: true });
  registration.waiting.postMessage("skipWaiting");
  await new Promise((resolve) => setTimeout(resolve, 500));
  return status();
}

export async function loadChunk() {
  const chunk = await import("./chunk.js");
  log(`page ${VERSION} loaded chunk ${chunk.VERSION}`);
  return `page ${VERSION} + chunk ${chunk.VERSION}`;
}

export async function loadBooks() {
  const started = performance.now();
  const response = await fetch("/api/books?delay=500");
  const books = await response.json();
  log(`books: ${books.length}, ${(performance.now() - started).toFixed(0)} ms, from ${response.headers.get("x-served-by") ?? "network"}`);
}

// The Cache API keys on the URL; the Authorization header is not part of the key unless the response says Vary
export async function perUserCache(vary) {
  const cache = await caches.open("per-user-demo");
  const url = `/api/me/orders${vary ? "?vary=1" : ""}`;
  const ada = new Request(url, { headers: { Authorization: "Bearer ada" } });
  await cache.put(ada, await fetch(ada));
  const alan = new Request(url, { headers: { Authorization: "Bearer alan" } });
  const hit = await cache.match(alan);
  log(`Vary: ${vary ? "Authorization" : "(none)"} -> Alan gets ${hit ? JSON.stringify(await hit.json()) : "nothing, a cache miss"}`);
}
