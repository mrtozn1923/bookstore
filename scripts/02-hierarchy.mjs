import { withPage } from './lib/browser.mjs';
import { collectTextStyles } from './lib/styles.mjs';
import { ratio, hex } from './lib/wcag.mjs';

// Style audit and "what stands out" table for the book detail page.
const keys = { title: '.title', price: '.price', 'add to cart': '#add', stock: '.stock', isbn: '.meta dd:nth-of-type(1)' };

await withPage(async ({ page, open }) => {
  for (const variant of ['before', 'after']) {
    await open(`web/02-book-detail/${variant}.html`);
    for (const [name, sel] of Object.entries(keys)) {
      await page.$eval(sel === keys.isbn ? 'dd:not(.stock)' : sel, (e, k) => (e.dataset.key = k), name);
    }
    const items = await page.evaluate(collectTextStyles);
    const distinct = (f) => new Set(items.map(f)).size;
    const bold = items.filter((i) => i.weight >= 600).length;
    const loud = await page.$$eval('button, a', (els) => els.map((e) => getComputedStyle(e).backgroundColor));
    const filled = loud.filter((c) => c !== 'rgba(0, 0, 0, 0)' && ratio(c.match(/\d+/g).map(Number), [255, 255, 255]) >= 3).length;
    console.log(`--- ${variant}: ${distinct((i) => i.size)} font sizes, ${distinct((i) => hex(i.color))} text colors, ` +
      `${bold} of ${items.length} text elements bold, ${filled} solid buttons`);
    for (const name of Object.keys(keys)) {
      const i = items.find((x) => x.key === name);
      console.log(`  ${name.padEnd(12)} ${String(i.size).padStart(2)}px ${i.weight}  contrast ${ratio(i.color, i.bg).toFixed(2).padStart(5)}  top ${String(i.top).padStart(3)}px`);
    }
  }
}, { width: 800, height: 700 });
