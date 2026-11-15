// Uses Bookstore's Angular Aria widgets and the Signal Forms checkout with the keyboard and prints what the
// accessibility tree says after each key.
// Usage: node widgets.mjs <combobox|tabs|form>
import { chromium } from 'playwright';
import { AxeBuilder } from '@axe-core/playwright';
import { focusedAx, open } from './lib.mjs';

const browser = await chromium.launch();
const page = await (await browser.newContext()).newPage();
const html = async selector => (await page.locator(selector).first().evaluate(el => el.outerHTML)).replace(/ _ngcontent-[\w-]+=""/g, '');
const mode = process.argv[2] ?? 'combobox';

if (mode === 'combobox') {
  await open(page, 'catalog');
  const box = page.getByRole('combobox', { name: 'Search books' });
  // Name, expanded state and the option aria-activedescendant points to, as a screen reader gets them
  const state = async () => {
    const active = await box.getAttribute('aria-activedescendant');
    return `${await focusedAx(page)} value="${await box.inputValue()}" expanded=${await box.getAttribute('aria-expanded')}` +
      (active ? ` active="${await page.locator(`#${active}`).textContent()}"` : '');
  };
  await box.focus();
  for (const [label, act] of [
    ['type "t"', () => page.keyboard.type('t')],
    ['type "he"', () => page.keyboard.type('he')],
    ['ArrowDown', () => page.keyboard.press('ArrowDown')],
    ['ArrowDown', () => page.keyboard.press('ArrowDown')],
    ['Escape', () => page.keyboard.press('Escape')],
    ['ArrowDown', () => page.keyboard.press('ArrowDown')],
    ['ArrowUp', () => page.keyboard.press('ArrowUp')],
    ['Enter', () => page.keyboard.press('Enter')],
  ]) {
    await act();
    await page.waitForTimeout(250);
    console.log(`${label.padEnd(10)} ${await state()}`);
  }
  console.log(`result: ${await page.getByTestId('book-count').textContent()}`);
  // A mouse user picks a suggestion with a click
  await box.fill('hob');
  await page.getByRole('option', { name: 'The Hobbit' }).click();
  await page.getByTestId('book-count').filter({ hasText: 'Hobbit' }).waitFor();
  console.log(`click "The Hobbit": ${await page.getByTestId('book-count').textContent()}, focus ${await focusedAx(page)}`);
  await box.fill('the');
  await page.waitForTimeout(250);
  console.log(await page.locator('.search-field').ariaSnapshot());
  // axe on the open popup: roles, required children, aria-activedescendant and aria-controls targets
  const axe = await new AxeBuilder({ page }).include('.search-field').withTags(['wcag2a', 'wcag2aa', 'wcag21aa', 'wcag22aa']).analyze();
  console.log(`axe (popup open): ${axe.violations.map(v => v.id).join(', ') || 'no violations'}, passed rules=${axe.passes.length}`);
}

if (mode === 'tabs') {
  await open(page, 'book');
  await page.getByRole('tab', { name: 'Description' }).waitFor();
  console.log(await page.locator('.book-tabs').ariaSnapshot());
  await page.getByRole('link', { name: 'Back to catalog' }).focus();
  for (const key of ['Tab', 'ArrowRight', 'ArrowRight', 'End', 'Home', 'Tab', 'Shift+Tab']) {
    await page.keyboard.press(key);
    await page.waitForTimeout(150);
    const panel = (await page.locator('[role=tabpanel]:not([inert])').innerText()).trim().replace(/\s+/g, ' ');
    console.log(`${key.padEnd(10)} ${(await focusedAx(page)).padEnd(26)} panel: ${panel.slice(0, 48)}`);
  }
}

if (mode === 'form') {
  await open(page, 'cart');
  const name = page.getByLabel('Full name');
  console.log(`before submit: ${await html('#customer-name')}`);
  await page.getByRole('button', { name: 'Place order' }).click();
  await page.getByTestId('error-summary').waitFor();
  console.log(`after submit:  ${await html('#customer-name')}`);
  await name.focus();
  console.log(`focus:         ${await focusedAx(page)}`);
}

await browser.close();
