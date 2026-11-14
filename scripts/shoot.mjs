import { mkdir } from 'node:fs/promises';
import { basename, dirname } from 'node:path';
import { withPage } from './lib/browser.mjs';

// Usage: node scripts/shoot.mjs web/01-checkout/before.html [width] [light|dark]
const [path, width = '1280', scheme = 'light'] = process.argv.slice(2);
const out = `out/${basename(dirname(path))}-${basename(path, '.html')}-${width}-${scheme}.png`;
await mkdir('out', { recursive: true });
await withPage(async ({ page, open }) => {
  await open(path);
  await page.screenshot({ path: out, fullPage: true });
  console.log(`saved ${out}`);
}, { width: Number(width), scale: 2, colorScheme: scheme });
