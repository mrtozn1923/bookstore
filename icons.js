const pw = require('playwright');
const start = require('./server');

(async () => {
  const servers = start();
  const browser = await pw.chromium.launch();
  const page = await browser.newPage();
  await page.goto('http://127.0.0.1:52113/book.html');

  // sprite'ı taşıyan svg ekranda yer kaplıyor mu?
  console.log('sprite box:', await page.locator('body > svg').evaluate(s => {
    const { width, height } = s.getBoundingClientRect();
    return `${width}x${height} display=${getComputedStyle(s).display}`;
  }));

  // Chromium'un kendi accessibility tree'si: role, name ve description
  const cdp = await page.context().newCDPSession(page);
  const { nodes } = await cdp.send('Accessibility.getFullAXTree');
  for (const n of nodes) {
    const role = n.role?.value;
    if (!['button', 'image', 'link'].includes(role) || n.ignored) continue;
    const desc = n.description?.value ? ` description="${n.description.value}"` : '';
    console.log(`${role} "${n.name?.value ?? ''}"${desc}`);
  }

  // Tab sırası: svg'lerden biri odak alıyor mu?
  const order = [];
  for (let i = 0; i < 8; i++) {
    await page.keyboard.press('Tab');
    order.push(await page.evaluate(() => {
      const el = document.activeElement;
      return el.id ? `${el.tagName.toLowerCase()}#${el.id}` : el.tagName.toLowerCase();
    }));
  }
  console.log('tab order:', order.join(' > '));
  await browser.close();
  servers.forEach(s => s.close());
})();
