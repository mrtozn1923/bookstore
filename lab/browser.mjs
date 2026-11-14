// Shared Chromium launcher for the lab scripts: Playwright's own Chromium, never an installed Chrome.
import { chromium, devices } from 'playwright';

export const chromiumArgs = [
  '--use-mock-keychain',
  '--host-resolver-rules=MAP *.thirdparty.test 127.0.0.1',
];

// Throttling profiles (applied with CDP, like DevTools does)
export const profiles = {
  desktop: { device: null, cpu: 1, latency: 40, downKbps: 10_000, upKbps: 5_000 },
  mobile: { device: 'Pixel 7', cpu: 4, latency: 150, downKbps: 1_600, upKbps: 750 },
  lowend: { device: 'Pixel 7', cpu: 6, latency: 300, downKbps: 700, upKbps: 400 },
};

// bfcache/prerender labs: Playwright starts Chromium with --disable-back-forward-cache, and its default
// headless shell has no bfcache; 'full' uses Playwright's full Chromium build in new headless mode instead
export function launch({ full = false } = {}) {
  return full
    ? chromium.launch({ channel: 'chromium', args: chromiumArgs, ignoreDefaultArgs: ['--disable-back-forward-cache'] })
    : chromium.launch({ args: chromiumArgs });
}

export async function newThrottledPage(browser, profileName) {
  const profile = profiles[profileName];
  const context = await browser.newContext(profile.device ? devices[profile.device] : { viewport: { width: 1350, height: 940 } });
  const page = await context.newPage();
  const cdp = await context.newCDPSession(page);
  await cdp.send('Network.emulateNetworkConditions', {
    offline: false,
    latency: profile.latency,
    downloadThroughput: (profile.downKbps * 1000) / 8,
    uploadThroughput: (profile.upKbps * 1000) / 8,
  });
  await cdp.send('Emulation.setCPUThrottlingRate', { rate: profile.cpu });
  return { context, page, cdp };
}

// Ends a lab visit like a user leaving: a trusted key press finalizes LCP, then the page is marked
// hidden so web-vitals reports CLS/INP and the app flushes its RUM queue with sendBeacon.
export async function finishVisit(page) {
  await page.keyboard.press('Shift');
  await page.waitForTimeout(1200);
  await page.evaluate(() => {
    Object.defineProperty(document, 'visibilityState', { value: 'hidden', configurable: true });
    document.dispatchEvent(new Event('visibilitychange'));
  });
  await page.waitForTimeout(500);
}

// Keeps a copy of every RUM beacon body in the page (window.__rum) so lab scripts can read it
export async function captureRum(page) {
  await page.addInitScript(() => {
    const send = navigator.sendBeacon.bind(navigator);
    window.__rum = [];
    navigator.sendBeacon = (target, data) => {
      if (String(target).endsWith('/api/rum')) data.text().then((t) => window.__rum.push(...JSON.parse(t)));
      return send(target, data);
    };
  });
  return () => page.evaluate(() => window.__rum);
}
