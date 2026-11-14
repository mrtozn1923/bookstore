import { withPage } from './lib/browser.mjs';

// The same customer fills in both forms (made-up data; the password is a 16-character placeholder).
const data = {
  firstName: 'Ada', lastName: 'Lovelace', fullName: 'Ada Lovelace', email: 'ada@example.com', email2: 'ada@example.com',
  password: 'x'.repeat(16), password2: 'x'.repeat(16), phone: '02125550100', address: '12 Moda Street',
  city: 'Istanbul', postcode: '34710', billName: 'Ada Lovelace', billAddress: '12 Moda Street', billCity: 'Istanbul', billPostcode: '34710',
};

const visibleError = (page, name) => page.evaluate((n) => {
  const field = document.querySelector(`[name="${n}"]`);
  const box = field.closest('.float, .field');
  return box.querySelector('.error')?.textContent || '(none)';
}, name);

await withPage(async ({ page, open }) => {
  for (const variant of ['before', 'after']) {
    const path = `web/06-forms/${variant}.html`;
    await open(path);
    const fields = await page.$$eval('input:not([type=checkbox]), select', (els) => els.filter((e) => e.offsetParent).map((e) => ({
      name: e.name,
      required: e.required || (e.labels[0]?.textContent.endsWith('*') ?? false),
      autocomplete: e.getAttribute('autocomplete'),
    })));
    const required = fields.filter((f) => f.required);
    const typed = required.reduce((n, f) => n + (data[f.name]?.length ?? 0), 0);

    // 1) typing "ada@" without leaving the field, then leaving it
    await page.click('#email');
    await page.keyboard.type('ada@', { delay: 50 });
    const whileTyping = await visibleError(page, 'email');
    await page.keyboard.press('Tab');
    const afterBlur = await visibleError(page, 'email');

    // 2) submitting an empty form
    await open(path);
    await page.click('button[type=submit]');
    const errors = await page.$$eval('.error', (els) => els.filter((e) => e.textContent).map((e) => e.textContent));
    const focused = await page.evaluate(() => document.activeElement.id || document.activeElement.tagName.toLowerCase());
    const title = await page.title();

    // 3) pasting into the password field
    if (variant === 'after') await page.check('#saveDetails');
    const pasteBlocked = await page.$eval('#password', (input) => {
      const dt = new DataTransfer();
      dt.setData('text/plain', 'from-password-manager');
      return !input.dispatchEvent(new ClipboardEvent('paste', { clipboardData: dt, cancelable: true, bubbles: true }));
    });

    console.log(`--- ${variant}: ${fields.length} visible fields, ${required.length} required, ` +
      `${typed} characters to type, ${fields.filter((f) => f.autocomplete).length} with autocomplete`);
    console.log(`  typing "ada@": error while typing "${whileTyping}", after leaving the field "${afterBlur}"`);
    console.log(`  empty submit: ${errors.length} messages ("${[...new Set(errors)].join('", "')}"), focus on ${focused}, title "${title}"`);
    console.log(`  paste into password blocked: ${pasteBlocked}`);
  }
}, { width: 800, height: 900 });
