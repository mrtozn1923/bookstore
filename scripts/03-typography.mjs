import { withPage } from './lib/browser.mjs';
import { collectTextStyles } from './lib/styles.mjs';
import { lineStats, decimalPositions } from './lib/text.mjs';
import { readFile } from 'node:fs/promises';

// Unicode ranges of the @fontsource subsets, e.g. "latin": "U+0000-00FF,U+0131,..."
const subsets = JSON.parse(await readFile('node_modules/@fontsource-variable/inter/unicode.json', 'utf8'));
const inSubset = (ch, name) => subsets[name].split(',').some((r) => {
  const [a, b = a] = r.slice(2).split('-').map((h) => parseInt(h, 16));
  return ch.codePointAt(0) >= a && ch.codePointAt(0) <= b;
});

await withPage(async ({ page, open }) => {
  for (const variant of ['before', 'after']) {
    await open(`web/03-typography/${variant}.html`);
    const sizes = new Set((await page.evaluate(collectTextStyles)).map((i) => i.size));
    const desc = await page.evaluate(lineStats, '.desc');
    const dots = await page.evaluate(decimalPositions, '.price');
    console.log(`--- ${variant} @1280px: font sizes ${[...sizes].sort((a, b) => a - b).join('/')}`);
    console.log(`  description: ${desc.fontSize}, line-height ${desc.lineHeight}, ${desc.lines} lines, ` +
      `avg ${desc.avg} chars/line, max ${desc.max}`);
    console.log(`  price decimal points x: ${dots.join(', ')} (spread ${(Math.max(...dots) - Math.min(...dots)).toFixed(1)}px)`);
  }

  // Turkish: which font actually draws the glyphs, and what uppercase produces
  await open('web/03-typography/turkish.html');
  const cdp = await page.context().newCDPSession(page);
  await cdp.send('DOM.enable');
  await cdp.send('CSS.enable');
  const { root } = await cdp.send('DOM.getDocument');
  for (const id of ['a1', 'a2']) {
    const { nodeId } = await cdp.send('DOM.querySelector', { nodeId: root.nodeId, selector: `#${id}` });
    const { fonts } = await cdp.send('CSS.getPlatformFontsForNode', { nodeId });
    const family = await page.$eval(`#${id}`, (e) => getComputedStyle(e).fontFamily.split(',')[0]);
    const web = fonts.filter((f) => f.isCustomFont).reduce((n, f) => n + f.glyphCount, 0);
    const fallback = fonts.filter((f) => !f.isCustomFont).reduce((n, f) => n + f.glyphCount, 0);
    console.log(`--- ${family}: web font ${web} glyphs, system fallback ${fallback} glyphs`);
  }
  const text = await page.textContent('#a1');
  console.log(`--- not in the latin subset: ${[...new Set(text)].filter((c) => !inSubset(c, 'latin')).join(' ')}`);
  for (const id of ['b1', 'b2']) {
    const r = await page.$eval(`#${id}`, (e) => [e.lang, e.innerText]);
    console.log(`--- uppercase lang="${r[0]}": ${r[1]}`);
  }
}, { width: 1280, height: 800 });
