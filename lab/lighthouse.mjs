// Runs Lighthouse N times against a URL (default mobile config, simulated throttling) and prints
// the score and the lab metrics of every run plus the median.
// Usage: node lighthouse.mjs <url> [--runs 5] [--preset desktop] [--save report.json]
import { parseArgs } from 'node:util';
import { writeFile } from 'node:fs/promises';
import lighthouse from 'lighthouse';
import * as chromeLauncher from 'chrome-launcher';
import { chromiumArgs } from './browser.mjs';

const { values, positionals } = parseArgs({
  allowPositionals: true,
  options: {
    runs: { type: 'string', default: '5' },
    preset: { type: 'string' },
    save: { type: 'string' },
  },
});
const url = positionals[0] ?? 'http://127.0.0.1:5300/';
const config = values.preset === 'desktop' ? (await import('lighthouse/core/config/desktop-config.js')).default : undefined;
const audits = ['first-contentful-paint', 'largest-contentful-paint', 'total-blocking-time', 'cumulative-layout-shift', 'speed-index'];
const rows = [];

for (let run = 1; run <= Number(values.runs); run++) {
  // CHROME_PATH points to Playwright's Chromium; chrome-launcher creates a fresh temp profile
  const chrome = await chromeLauncher.launch({ chromeFlags: ['--headless=new', ...chromiumArgs] });
  const result = await lighthouse(url, { port: chrome.port, onlyCategories: ['performance'], output: 'json' }, config);
  await chrome.kill();
  const lhr = result.lhr;
  rows.push([Math.round(lhr.categories.performance.score * 100), ...audits.map((a) => lhr.audits[a].numericValue)]);
  if (values.save && run === 1) await writeFile(values.save, result.report);
}

const fmt = (v, i) => (i === 4 ? v.toFixed(3) : i === 0 ? String(v) : String(Math.round(v)));
console.log(`Lighthouse ${rows.length} runs, ${values.preset ?? 'mobile'}: ${url}`);
console.log('run  score    FCP    LCP    TBT    CLS     SI');
rows.forEach((r, i) => console.log(`${String(i + 1).padEnd(5)}${r.map((v, j) => fmt(v, j).padStart(j ? 7 : 5)).join('')}`));
const med = rows[0].map((_, j) => rows.map((r) => r[j]).sort((a, b) => a - b)[Math.floor((rows.length - 1) / 2)]);
console.log(`med  ${med.map((v, j) => fmt(v, j).padStart(j ? 7 : 5)).join('')}`);
