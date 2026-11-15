// Submits the order form with mistakes and prints what a screen reader gets on the focused field.
// Usage: node form.mjs
import { chromium } from 'playwright';
import { focusedAx, open } from './lib.mjs';

const browser = await chromium.launch();
const page = await (await browser.newContext()).newPage();
await open(page, 'cart');

// Name left empty, e-mail without a domain; submit with Enter like a keyboard user
await page.getByLabel('E-mail').fill('ada');
await page.getByLabel('E-mail').press('Enter');
await page.waitForTimeout(300);
console.log(`after submit: ${await focusedAx(page)}`);
await page.keyboard.press('Tab');
console.log(`next Tab:     ${await focusedAx(page)}`);

// Fix both fields and submit again
await page.getByLabel('Full name').fill('Ada Lovelace');
await page.getByLabel('E-mail').fill('ada@example.com');
await page.getByLabel('E-mail').press('Enter');
console.log(`result: ${await page.getByTestId('order-confirmation').textContent({ timeout: 3000 }).then(t => t.trim().replace(/\s+/g, ' '))}`);
await browser.close();
