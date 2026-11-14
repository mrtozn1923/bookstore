// A framework-free service worker for Bookstore's catalog (scope /sw/)
const VERSION = "v1";
const STATIC_CACHE = `static-${VERSION}`;
const API_CACHE = "api";
const SHELL = ["./", "./app.js", "./chunk.js", "./styles.css"];

// tell every page of this scope what happened (so the lifecycle is visible in the page)
async function tell(text) {
  for (const client of await self.clients.matchAll({ includeUncontrolled: true })) {
    client.postMessage({ version: VERSION, text });
  }
}

self.addEventListener("install", (event) => {
  event.waitUntil(
    caches
      .open(STATIC_CACHE)
      .then((cache) => cache.addAll(SHELL)) // if one file fails, the install fails and the old version stays
      .then(() => tell(`install: precached ${SHELL.length} files into ${STATIC_CACHE}`)),
  );
});

self.addEventListener("activate", (event) => {
  event.waitUntil(
    (async () => {
      for (const key of await caches.keys()) {
        if (key.startsWith("static-") && key !== STATIC_CACHE) {
          await caches.delete(key); // safe only now: no page uses the old version any more
          await tell(`activate: deleted ${key}`);
        }
      }
      await self.registration.navigationPreload?.enable();
      await tell("activate: done");
    })(),
  );
});

self.addEventListener("message", (event) => {
  if (event.data === "version") event.ports[0].postMessage(VERSION);
  if (event.data === "skipWaiting") self.skipWaiting();
});

self.addEventListener("fetch", (event) => {
  const { request } = event;
  const url = new URL(request.url);
  if (request.method !== "GET" || url.pathname.startsWith("/api/me/")) {
    return; // network-only: no respondWith, the browser handles it as if there were no service worker
  }
  if (request.mode === "navigate") {
    event.respondWith(networkFirst(event));
  } else if (url.pathname === "/api/books") {
    event.respondWith(staleWhileRevalidate(event));
  } else if (url.origin === location.origin && url.pathname.startsWith("/sw/")) {
    event.respondWith(cacheFirst(request));
  }
});

// static files of this version: from this version's cache, the network only for misses
async function cacheFirst(request) {
  const cache = await caches.open(STATIC_CACHE); // not caches.match(): that would search every version
  const cached = await cache.match(request);
  if (cached) return cached;
  const response = await fetch(request);
  if (response.ok) await cache.put(request, response.clone());
  return response;
}

// pages: fresh when possible, the cached shell when offline
async function networkFirst(event) {
  try {
    const response = (await event.preloadResponse) ?? (await fetch(event.request));
    return response;
  } catch {
    const cache = await caches.open(STATIC_CACHE);
    return (await cache.match(event.request)) ?? (await cache.match("./"));
  }
}

// catalog: answer from the cache at once, refresh the cache in the background
async function staleWhileRevalidate(event) {
  const cache = await caches.open(API_CACHE);
  const cached = await cache.match(event.request);
  // no-cache: ask the server (a cheap 304 with ETag); without it the HTTP cache may answer for max-age seconds
  const refresh = fetch(event.request, { cache: "no-cache" }).then(async (response) => {
    if (response.ok) {
      await cache.put(event.request, response.clone());
      await tell(`cache updated: ${new URL(event.request.url).pathname}`);
    }
    return response;
  });
  event.waitUntil(refresh.catch(() => {})); // keep the worker alive until the refresh is done
  if (!cached) return refresh;
  const headers = new Headers(cached.headers);
  headers.set("x-served-by", "service worker cache");
  return new Response(cached.body, { status: cached.status, headers });
}
