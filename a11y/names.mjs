// Shows how Chromium computes the accessible name of each element in demos/names.html, and where it came from.
// Usage: node names.mjs
import { readFileSync } from 'node:fs';
import { chromium } from 'playwright';
import { AxeBuilder } from '@axe-core/playwright';

const browser = await chromium.launch();
const page = await (await browser.newContext()).newPage();
await page.setContent(readFileSync(new URL('./demos/names.html', import.meta.url), 'utf8'));

const cdp = await page.context().newCDPSession(page);
const { nodes } = await cdp.send('Accessibility.getFullAXTree');
const label = s => s.attribute ?? s.nativeSource ?? s.type;
for (const id of ['all', 'aria-label', 'hidden-text', 'described', 'title-only', 'placeholder-only',
  'icon-only', 'icon-labelled', 'arrow', 'arrow-hidden', 'div-button']) {
  const { node } = await cdp.send('DOM.describeNode', { objectId: (await cdp.send('Runtime.evaluate',
    { expression: `document.getElementById('${id}')` })).result.objectId });
  const ax = nodes.find(n => n.backendDOMNodeId === node.backendNodeId);
  const sources = ax.name?.sources ?? [];
  const used = sources.find(s => s.value?.value && !s.superseded);
  const ignored = sources.filter(s => s.value?.value && s.superseded).map(label);
  const description = ax.description?.value ? ` description="${ax.description.value}"` : '';
  console.log(`${id.padEnd(17)} ${ax.role.value} "${ax.name?.value ?? ''}" <- ${used ? label(used) : '(none)'}` +
    (ignored.length ? ` (ignored: ${ignored.join(', ')})` : '') + description);
}

// role="button" changes what the element is called, not what it does
await page.focus('#div-button');
await page.keyboard.press('Enter');
await page.keyboard.press('Space');
console.log(`div-button: Enter + Space -> clicks=${await page.locator('#div-button').getAttribute('data-clicks') ?? 0}`);

const axe = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa', 'experimental']).analyze();
console.log('axe:', axe.violations.map(v => `${v.id} (${v.nodes.map(n => n.target.join(' ')).join(', ')})`).join('; ') || 'no violations');
await browser.close();
