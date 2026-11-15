const pw = require('playwright');
const start = require('./server');

const base = 'http://127.0.0.1:52113/book.html';

(async () => {
  const servers = start();
  for (const name of ['chromium', 'webkit']) {
    const browser = await pw[name].launch();
    const page = await browser.newPage();
    // her sayfa yüklemesinde beforematch event'lerini kaydet
    await page.addInitScript(() => {
      window.matches = [];
      document.addEventListener('beforematch', e => matches.push(e.target.id || e.target.tagName), true);
    });
    const state = () => page.evaluate(() =>
      `toc hidden=${JSON.stringify(document.getElementById('toc').getAttribute('hidden'))} ` +
      `details open=${document.querySelector('details').open} beforematch=[${matches}]`);

    await page.goto(base);
    console.log(`${name} load:          ${await state()}`);
    await page.evaluate(() => window.find('Riddles in the Dark'));
    console.log(`${name} window.find:   ${await state()}`);
    await page.goto('about:blank');
    await page.goto(base + '#:~:text=Riddles%20in%20the%20Dark');
    console.log(`${name} text fragment: ${await state()}`);
    await page.goto('about:blank');
    await page.goto(base + '#:~:text=original%20maps');
    console.log(`${name} details text:  ${await state()}`);
    await browser.close();
  }
  servers.forEach(s => s.close());
})();
