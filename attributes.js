const pw = require('playwright');
const start = require('./server');

(async () => {
  const servers = start();
  for (const name of ['chromium', 'webkit']) {
    const browser = await pw[name].launch();
    const page = await browser.newPage();
    await page.goto('http://127.0.0.1:52113/book.html');
    const book = await page.evaluate(() => {
      const article = document.querySelector('article');
      // data-stock-count → dataset.stockCount, değer her zaman string
      const { isbn, stockCount } = article.dataset;
      return `dataset: isbn=${isbn} stockCount=${JSON.stringify(stockCount)}\n  ` +
        `translate: h1=${document.querySelector('h1').translate} isbn=${document.querySelector('.isbn').translate} aside=${document.querySelector('aside').translate}\n  ` +
        `draggable: a=${document.querySelector('aside a').draggable} p=${document.querySelector('aside p').draggable}`;
    });
    await page.goto('http://127.0.0.1:52113/admin-note.html');
    const form = await page.evaluate(() =>
      ['note', 'recipient', 'coupon', 'courier'].map(id => {
        const el = document.getElementById(id);
        return `${id}: autocapitalize="${el.autocapitalize}" spellcheck=${el.spellcheck}`;
      }).join('\n  '));
    console.log(`${name} ${browser.version()}\n  ${book}\n  ${form}`);
    await browser.close();
  }
  servers.forEach(s => s.close());
})();
