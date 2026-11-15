// Writes the test script for a manual screen reader session: Bookstore's search -> filter -> book -> cart ->
// checkout flow, the keys to press in NVDA and VoiceOver, and what the accessibility tree says a screen reader
// should get at each step. The "heard" columns are filled in by hand during the session.
// Usage: node sr-script.mjs
import { chromium } from 'playwright';
import { base, focusedAx } from './lib.mjs';
import { recorder } from './recorder.mjs';

const browser = await chromium.launch();
const context = await browser.newContext();
await context.addInitScript(recorder);
const page = await context.newPage();
const cdp = await context.newCDPSession(page);
const rows = [];

// A node with this role (and name) from Chromium's tree, as browse mode would reach it
async function nodeText(role, name) {
  const { nodes } = await cdp.send('Accessibility.getFullAXTree');
  const n = nodes.find(x => !x.ignored && x.role?.value === role && (name === undefined || x.name?.value === name));
  if (!n) return `(no ${role}${name ? ` "${name}"` : ''} in the tree)`;
  const level = n.properties?.find(p => p.name === 'level')?.value.value;
  return `${role}${level ? ` level ${level}` : ''} "${n.name?.value ?? ''}"`;
}

// expected() runs after a short wait: focus can move a moment after the new page has rendered
async function step(task, nvda, voiceOver, expected = async () => '') {
  await page.waitForTimeout(400);
  const said = await page.evaluate(() => window.__announcements.splice(0));
  rows.push([task, nvda, voiceOver, [await expected(), ...said].filter(Boolean).join(' + ')]);
}

await page.goto(base + '/');
await page.locator('.book-card').first().waitFor();
await step('open the catalog', 'page loads', 'page loads', async () => `title "${await page.title()}"`);

await step('jump to the search', 'D', 'VO+U, Landmarks', async () => await nodeText('search'));

const search = page.getByRole('searchbox', { name: 'Search books' });
await search.focus();
await step('go to the search field', 'E', 'VO+Command+J', async () => await focusedAx(page));
await search.fill('dune');
await search.press('Enter');
await page.getByTestId('book-count').filter({ hasText: 'dune' }).waitFor();
await step('type "dune", Enter', 'type, Enter', 'type, Return');

await page.getByRole('radio', { name: 'All' }).focus();
await step('go to the categories', 'R', 'VO+Command+J', async () => await focusedAx(page));
const response = page.waitForResponse(r => r.url().includes('category=science-fiction'));
await page.getByRole('radio', { name: 'Science Fiction' }).focus();
await page.keyboard.press('Space');
await response;
await step('select Science Fiction', 'R R, Space', 'VO+Command+J x2, VO+Space', async () => await focusedAx(page));

await step('find the book', 'H', 'VO+Command+H', async () => await nodeText('heading', 'Dune'));

const link = page.getByRole('link', { name: 'Dune', exact: true });
await link.focus();
await link.press('Enter');
await page.getByRole('heading', { level: 1, name: 'Dune' }).waitFor();
await step('open the book', 'Enter', 'VO+Space', async () => `${await focusedAx(page)}, title "${await page.title()}"`);

await page.getByRole('button', { name: 'Add to cart' }).focus();
await page.keyboard.press('Enter');
await page.getByText('Added.').waitFor();
await step('add to the cart', 'B, Enter', 'Tab Tab, VO+Space', async () => await focusedAx(page));

await page.getByRole('link', { name: 'Go to cart' }).focus();
await page.keyboard.press('Enter');
await page.getByRole('heading', { level: 1, name: 'Your cart' }).waitFor();
await step('go to the cart', 'Tab, Enter', 'Tab, VO+Space', async () => `${await focusedAx(page)}, title "${await page.title()}"`);

await page.getByLabel('Full name').focus();
await step('go to the form', 'F until "Full name"', 'VO+Command+J until "Full name"', async () => await focusedAx(page));
await page.getByLabel('Full name').fill('Ada Lovelace');
await page.keyboard.press('Tab');
await page.keyboard.type('ada@example.com');
await page.keyboard.press('Enter');
await page.getByTestId('order-confirmation').waitFor();
await step('fill in, Enter', 'type, Tab, type, Enter', 'type, Tab, type, Return', async () => await focusedAx(page));

const width = rows.reduce((w, r) => r.map((c, i) => Math.max(w[i] ?? 0, c.length)), []);
console.log(['task', 'NVDA', 'VoiceOver', 'expected from the tree'].map((c, i) => c.padEnd(width[i])).join(' | ').trimEnd());
rows.forEach((r, i) => console.log(`${r.map((c, j) => c.padEnd(width[j])).join(' | ').trimEnd()}`));
await browser.close();
