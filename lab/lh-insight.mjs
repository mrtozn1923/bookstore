// Prints one Lighthouse insight from a saved JSON report (lighthouse.mjs --save report.json).
// Usage: node lh-insight.mjs report.json render-blocking-insight
import { readFile } from 'node:fs/promises';

const [file, id] = process.argv.slice(2);
const lhr = JSON.parse(await readFile(file, 'utf8'));
const audit = lhr.audits[id];
console.log(`${audit.title}: ${audit.displayValue ?? '-'}`);
for (const item of audit.details?.items ?? []) {
  const url = typeof item.url === 'string' ? item.url.replace(/^https?:\/\//, '') : '';
  const rest = Object.entries(item)
    .filter(([k, v]) => k !== 'url' && typeof v !== 'object')
    .map(([k, v]) => `${k}=${typeof v === 'number' ? Math.round(v) : v}`);
  console.log(`  ${url} ${rest.join(' ')}`.trimEnd());
}
