// Prints every layout shift of one throttled visit: time, score, hadRecentInput and the moved elements
// (previousRect -> currentRect), recomputes impact x distance for the largest shift and groups the
// shifts into session windows (gap < 1 s, window <= 5 s) like CLS does.
// Usage: node cls-trace.mjs <url> [--profile mobile] [--scroll 3] [--click "button.more"]
import { parseArgs } from 'node:util';
import { launch, newThrottledPage } from './browser.mjs';

const { values, positionals } = parseArgs({
  allowPositionals: true,
  options: {
    profile: { type: 'string', default: 'mobile' },
    scroll: { type: 'string', default: '0' },
    click: { type: 'string' },
  },
});
const browser = await launch();
const { page } = await newThrottledPage(browser, values.profile);
await page.addInitScript(() => {
  window.__shifts = [];
  new PerformanceObserver((list) => {
    for (const e of list.getEntries()) {
      window.__shifts.push({
        time: Math.round(e.startTime),
        value: e.value,
        hadRecentInput: e.hadRecentInput,
        sources: e.sources.map((s) => ({
          node: s.node ? s.node.nodeName.toLowerCase() + (s.node.className ? '.' + String(s.node.className).split(' ')[0] : '') : '(removed)',
          prev: [s.previousRect.x, s.previousRect.y, s.previousRect.width, s.previousRect.height].map(Math.round),
          curr: [s.currentRect.x, s.currentRect.y, s.currentRect.width, s.currentRect.height].map(Math.round),
        })),
      });
    }
  }).observe({ type: 'layout-shift', buffered: true });
});
await page.goto(positionals[0], { waitUntil: 'networkidle', timeout: 120_000 });
// A user who reads on: scroll down in steps of 400 px (a wheel scroll is not "recent input")
for (let i = 0; i < Number(values.scroll); i++) {
  await page.mouse.wheel(0, 400);
  await page.waitForTimeout(600);
}
if (values.click) {
  await page.locator(values.click).click();
}
await page.waitForTimeout(2000);
const vp = page.viewportSize();
const shifts = await page.evaluate(() => window.__shifts);
await browser.close();

console.log(`viewport ${vp.width}x${vp.height}`);
console.log('time(ms)   score   recentInput  element: previous [x,y,w,h] -> current');
for (const s of shifts) {
  s.sources.forEach((src, i) => console.log(
    `${i ? ''.padEnd(32) : `${String(s.time).padStart(8)}  ${s.value.toFixed(4)}  ${String(s.hadRecentInput).padEnd(11)}`}  ${src.node}: [${src.prev}] -> [${src.curr}]`));
}

// impact fraction = visible union of old and new rects / viewport area;
// distance fraction = largest move / larger viewport side
const counted = shifts.filter((s) => !s.hadRecentInput);
const largest = counted.toSorted((a, b) => b.value - a.value)[0];
if (largest) {
  const clip = ([x, y, w, h]) => [Math.max(0, x), Math.max(0, y), Math.min(vp.width, x + w), Math.min(vp.height, y + h)];
  const rects = largest.sources.flatMap((s) => [clip(s.prev), clip(s.curr)]).filter(([x1, y1, x2, y2]) => x2 > x1 && y2 > y1);
  const xs = [...new Set(rects.flatMap((r) => [r[0], r[2]]))].sort((a, b) => a - b);
  const ys = [...new Set(rects.flatMap((r) => [r[1], r[3]]))].sort((a, b) => a - b);
  let area = 0;
  for (let i = 0; i < xs.length - 1; i++) {
    for (let j = 0; j < ys.length - 1; j++) {
      if (rects.some((r) => r[0] <= xs[i] && xs[i + 1] <= r[2] && r[1] <= ys[j] && ys[j + 1] <= r[3])) area += (xs[i + 1] - xs[i]) * (ys[j + 1] - ys[j]);
    }
  }
  const impact = area / (vp.width * vp.height);
  // An element that moved out of the viewport has an empty rect; its distance is not measurable here
  const moved = largest.sources.filter((s) => s.prev[2] * s.prev[3] > 0 && s.curr[2] * s.curr[3] > 0);
  const move = Math.max(0, ...moved.map((s) => Math.max(Math.abs(s.curr[0] - s.prev[0]), Math.abs(s.curr[1] - s.prev[1]))));
  const distance = move / Math.max(vp.width, vp.height);
  console.log(`largest shift at ${largest.time} ms: impact ${impact.toFixed(4)} x distance ${distance.toFixed(4)} = ${(impact * distance).toFixed(4)} (browser: ${largest.value.toFixed(4)})`);
}

const windows = [];
for (const s of counted) {
  const w = windows.at(-1);
  if (w && s.time - w.last < 1000 && s.time - w.first < 5000) {
    w.value += s.value; w.last = s.time; w.count++;
  } else {
    windows.push({ first: s.time, last: s.time, value: s.value, count: 1 });
  }
}
windows.forEach((w, i) => console.log(`session window ${i + 1}: ${w.first}-${w.last} ms, ${w.count} shift(s), ${w.value.toFixed(4)}`));
console.log(`CLS = largest session window = ${Math.max(0, ...windows.map((w) => w.value)).toFixed(4)}; excluded (recent input): ${shifts.length - counted.length}`);
