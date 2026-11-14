import { withPage } from './lib/browser.mjs';
import { collectTextStyles } from './lib/styles.mjs';
import { ratio, hex } from './lib/wcag.mjs';

// WCAG 2.2: 4.5:1 for text, 3:1 for large text (24px, or 18.66px bold) and for UI component boundaries.
const needed = (i) => (i.size >= 24 || (i.size >= 18.66 && i.weight >= 700) ? 3 : 4.5);

await withPage(async ({ page, open }) => {
  for (const scheme of ['light', 'dark']) {
    await page.emulateMedia({ colorScheme: scheme });
    for (const variant of ['before', 'after']) {
      await open(`web/04-color/${variant}.html`);
      await page.$$eval('.btn', (els) => els.forEach((e) => (e.dataset.key = 'control')));
      const items = await page.evaluate(collectTextStyles);
      const failing = items.filter((i) => ratio(i.color, i.bg) < needed(i));
      const borders = items.filter((i) => i.key === 'control').map((i) => ratio(i.border, i.parentBg));
      const colorOnly = await page.locator('.dot').count();
      await page.addScriptTag({ path: 'node_modules/axe-core/axe.min.js' });
      const axe = await page.evaluate(async () =>
        (await window.axe.run(document, { runOnly: ['color-contrast'] })).violations.flatMap((v) => v.nodes).length);

      console.log(`--- ${variant} / ${scheme}: ${failing.length} of ${items.length} texts below WCAG, axe color-contrast nodes: ${axe}`);
      for (const i of failing) {
        console.log(`  text    ${i.text.padEnd(14)} ${hex(i.color)} on ${hex(i.bg)}  ${ratio(i.color, i.bg).toFixed(2)} < ${needed(i)}`);
      }
      console.log(`  button  border vs card, lowest ${Math.min(...borders).toFixed(2)} (needs 3)`);
      if (colorOnly) console.log(`  stock   ${colorOnly} statuses shown only as a colored dot`);
    }
  }
}, { width: 640, height: 600 });
