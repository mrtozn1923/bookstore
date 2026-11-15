const { chromium } = require('playwright');
const start = require('./server');

(async () => {
  const servers = start();
  const browser = await chromium.launch();
  const context = await browser.newContext({ permissions: ['clipboard-read', 'clipboard-write'] });
  const page = await context.newPage();
  await page.goto('http://127.0.0.1:52113/admin-note.html');

  // aynı yapıştırma ve Ctrl+B, iki contenteditable değeriyle
  for (const mode of ['plaintext-only', 'true']) {
    await page.evaluate(async m => {
      const note = document.getElementById('note');
      note.contentEditable = m;
      note.textContent = '';
      await navigator.clipboard.write([new ClipboardItem({
        'text/html': new Blob(['<b>Call</b> before <i>17:00</i>'], { type: 'text/html' }),
        'text/plain': new Blob(['Call before 17:00'], { type: 'text/plain' }),
      })]);
    }, mode);
    await page.locator('#note').click();
    await page.keyboard.press('ControlOrMeta+v');
    await page.keyboard.press('ControlOrMeta+b');
    await page.keyboard.type(' urgent');
    console.log(`${mode.padEnd(14)} innerHTML: ${await page.locator('#note').innerHTML()}`);
  }

  // form gönderiminde hangi alanlar sunucuya gidiyor?
  await page.fill('#recipient', 'Ada Lovelace');
  await page.fill('#coupon', 'HOBBIT10');
  await page.getByRole('button', { name: 'Save' }).click();
  await page.waitForLoadState();

  // düzeltilmiş sayfa: not bir textarea
  await page.goto('http://127.0.0.1:52113/admin-note-fixed.html');
  await page.fill('#note', 'Call before 17:00');
  await page.getByRole('button', { name: 'Save' }).click();
  await page.waitForLoadState();
  await browser.close();
  servers.forEach(s => s.close());
})();
