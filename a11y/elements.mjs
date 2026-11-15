// Prints the lists a screen reader builds from the accessibility tree (like NVDA's Elements List or the
// VoiceOver rotor): landmarks, headings, links, buttons and form fields of one page.
// Usage: node elements.mjs <catalog|book|cart|statement>
import { chromium } from 'playwright';
import { open } from './lib.mjs';

const landmarkRoles = ['banner', 'navigation', 'main', 'contentinfo', 'search', 'region', 'form', 'complementary'];
const fieldRoles = ['textbox', 'searchbox', 'spinbutton', 'combobox', 'radio', 'checkbox', 'listbox'];

const name = process.argv[2] ?? 'catalog';
const browser = await chromium.launch();
const page = await (await browser.newContext()).newPage();
await open(page, name);
await page.waitForTimeout(300);

const cdp = await page.context().newCDPSession(page);
// The full tree from Chromium; "ignored" nodes (divs, spans without meaning) are not shown to a screen reader
const { nodes } = await cdp.send('Accessibility.getFullAXTree');
// Walk the tree from the root so the lists keep the reading order of the page
const byId = new Map(nodes.map(n => [n.nodeId, n]));
const ordered = [];
const walk = n => { ordered.push(n); (n.childIds ?? []).forEach(id => byId.has(id) && walk(byId.get(id))); };
walk(nodes.find(n => !n.parentId));
const visible = ordered.filter(n => !n.ignored);
const prop = (n, key) => n.properties?.find(p => p.name === key)?.value.value;
const label = n => `"${n.name?.value ?? ''}"`;

const lists = {
  landmarks: visible.filter(n => landmarkRoles.includes(n.role?.value)).map(n => `${n.role.value} ${label(n)}`),
  headings: visible.filter(n => n.role?.value === 'heading').map(n => `h${prop(n, 'level')} ${label(n)}`),
  links: visible.filter(n => n.role?.value === 'link').map(label),
  buttons: visible.filter(n => n.role?.value === 'button').map(label),
  'form fields': visible.filter(n => fieldRoles.includes(n.role?.value))
    .map(n => `${n.role.value} ${label(n)}${prop(n, 'required') ? ' required' : ''}${prop(n, 'checked') === 'true' ? ' checked' : ''}`),
};
console.log(`${name}: "${await page.title()}"`);
for (const [list, items] of Object.entries(lists)) {
  console.log(`  ${list} (${items.length}): ${items.length > 6 ? [...items.slice(0, 5), `… ${items.at(-1)}`].join(', ') : items.join(', ')}`);
}
await browser.close();
