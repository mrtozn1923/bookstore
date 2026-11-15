// axe on every page and on the states that only exist after an interaction (popup open, errors shown, dialog open).
// A violation that is not in a11y-baseline.json fails the test; known debt in the baseline does not.
// Update the baseline on purpose: A11Y_UPDATE_BASELINE=1 npx playwright test
import { AxeBuilder } from '@axe-core/playwright';
import { expect, test, type Page } from '@playwright/test';
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';

const tags = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa', 'best-practice'];
const baselineFile = new URL('../a11y-baseline.json', import.meta.url);
const baseline: string[] = JSON.parse(readFileSync(baselineFile, 'utf8'));
const found: string[] = [];

async function addToCart(page: Page, isbn: string): Promise<void> {
  await page.goto(`/books/${isbn}`);
  await page.getByRole('button', { name: 'Add to cart' }).click();
  await page.getByText('Added.').waitFor();
}

const states: { name: string; open: (page: Page) => Promise<void> }[] = [
  { name: 'catalog', open: async page => { await page.goto('/'); await page.locator('.book-card').first().waitFor(); } },
  { name: 'catalog, suggestions open', open: async page => {
    await page.goto('/');
    await page.getByRole('combobox', { name: 'Search books' }).fill('the');
    await page.getByRole('listbox', { name: 'Suggestions' }).waitFor();
  } },
  { name: 'book, details tab', open: async page => {
    await page.goto('/books/9780441172719');
    await page.getByRole('tab', { name: 'Details' }).click();
    await page.getByText('ISBN 9780441172719').waitFor();
  } },
  { name: 'cart, error summary', open: async page => {
    await addToCart(page, '9780441172719');
    await page.goto('/cart');
    await page.getByRole('button', { name: 'Place order' }).click();
    await page.getByTestId('error-summary').waitFor();
  } },
  { name: 'cart, remove dialog', open: async page => {
    await addToCart(page, '9780441172719');
    await page.goto('/cart');
    await page.getByRole('button', { name: 'Remove Dune' }).click();
    await page.getByRole('dialog').waitFor();
  } },
  { name: 'statement', open: async page => { await page.goto('/accessibility'); await page.locator('h1').waitFor(); } },
];

for (const state of states) {
  test(`${state.name}: no new axe violations`, async ({ page }) => {
    await state.open(page);
    const results = await new AxeBuilder({ page }).withTags(tags).analyze();
    // Raw results for the report (report.mjs)
    mkdirSync('reports/axe', { recursive: true });
    writeFileSync(`reports/axe/${state.name.replace(/\W+/g, '-')}.json`, JSON.stringify(results));

    // One line per element: state | rule | CSS target. Stable enough to compare between runs.
    const lines = results.violations.flatMap(v => v.nodes.map(n => `${state.name} | ${v.id} | ${n.target.join(' ')}`));
    found.push(...lines);
    const fresh = lines.filter(line => !baseline.includes(line));
    if (!process.env.A11Y_UPDATE_BASELINE) {
      expect(fresh, 'violations that are not in a11y-baseline.json').toEqual([]);
    }
  });
}

test.afterAll(() => {
  if (process.env.A11Y_UPDATE_BASELINE) {
    writeFileSync(baselineFile, JSON.stringify([...new Set(found)].sort(), null, 2) + '\n');
  }
  // Fixed debt: tell the team to remove the line from the baseline, so it cannot come back unnoticed
  const fixed = baseline.filter(line => !found.includes(line));
  if (fixed.length && !process.env.A11Y_UPDATE_BASELINE) {
    console.log(`Fixed, remove from a11y-baseline.json:\n  ${fixed.join('\n  ')}`);
  }
});
