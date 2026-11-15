// Scans Bookstore's pages with axe-core and prints the violations per page.
// Usage: node scan.mjs   (BOOKSTORE_URL defaults to http://127.0.0.1:52411)
import { chromium } from 'playwright';
import { AxeBuilder } from '@axe-core/playwright';
import { open, pages } from './lib.mjs';

const wcagTags = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'];
const browser = await chromium.launch();
// AxeBuilder needs a browser context; a page from browser.newPage() throws
const page = await (await browser.newContext()).newPage();
let total = 0;
for (const name of Object.keys(pages)) {
  const { path } = await open(page, name);
  const wcag = await new AxeBuilder({ page }).withTags(wcagTags).analyze();
  const all = await new AxeBuilder({ page }).analyze();
  const bestPractice = all.violations.filter(v => !v.tags.some(t => wcagTags.includes(t)));
  console.log(`--- ${name} (${path})`);
  for (const v of wcag.violations) {
    const sc = v.tags.filter(t => /^wcag\d{3,4}$/.test(t)).map(t => `${t[4]}.${t[5]}.${t.slice(6)}`).join(',');
    console.log(`  WCAG ${v.id.padEnd(20)} ${v.impact.padEnd(9)} SC ${sc.padEnd(7)} nodes=${v.nodes.length}`);
  }
  for (const v of bestPractice) {
    console.log(`  BP   ${v.id.padEnd(20)} ${v.impact.padEnd(9)} ${''.padEnd(10)} nodes=${v.nodes.length}`);
  }
  console.log(`  passed rules=${wcag.passes.length}`);
  total += wcag.violations.length;
}
console.log(`axe-core ${(await new AxeBuilder({ page }).analyze()).testEngine.version}, WCAG violations: ${total}`);
await browser.close();
