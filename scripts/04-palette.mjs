import { toSrgb } from './lib/color.mjs';
import { ratio, hex } from './lib/wcag.mjs';
import { withPage } from './lib/browser.mjs';

// 11-step scales: same lightness steps for every hue, chroma highest in the middle.
const steps = { 50: 0.97, 100: 0.93, 200: 0.87, 300: 0.79, 400: 0.7, 500: 0.62, 600: 0.54, 700: 0.47, 800: 0.4, 900: 0.33, 950: 0.25 };
const hues = { blue: [264, 0.2], gray: [264, 0.012], green: [150, 0.16], amber: [75, 0.17], red: [27, 0.2] };
const white = [255, 255, 255];

const css = [];
const ours = [];
for (const [name, [h, peak]] of Object.entries(hues)) {
  const row = [];
  for (const [step, l] of Object.entries(steps)) {
    const c = peak * Math.max(0.15, 1 - Math.abs(l - 0.6) * 1.8);
    const { rgb, chroma } = toSrgb(l, c, h);
    css.push(`  --${name}-${step}: oklch(${l} ${chroma} ${h}); /* ${hex(rgb)} */`);
    ours.push([`oklch(${l} ${chroma} ${h})`, rgb]);
    row.push(`${step}:${ratio(rgb, white).toFixed(2)}`);
  }
  console.log(`${name.padEnd(5)} vs white  ${row.join(' ')}`);
}
if (process.argv.includes('--css')) console.log(css.join('\n'));

// Cross-check: let Chromium convert the same oklch() values and compare the bytes.
if (process.argv.includes('--check')) {
  const diff = await withPage(({ page }) => page.evaluate((list) => {
    const ctx = document.createElement('canvas').getContext('2d', { willReadFrequently: true });
    return Math.max(...list.map(([css, rgb]) => {
      ctx.fillStyle = css;
      ctx.fillRect(0, 0, 1, 1);
      const px = ctx.getImageData(0, 0, 1, 1).data;
      return Math.max(...rgb.map((v, i) => Math.abs(v - px[i])));
    }));
  }, ours));
  console.log(`Chromium check: ${ours.length} colors, largest channel difference ${diff}/255`);
}
