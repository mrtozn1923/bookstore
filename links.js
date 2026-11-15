const { chromium } = require('playwright');
const start = require('./server');

(async () => {
  const servers = start();
  // headless shell'de PDF görüntüleyici yok; PDF'e gitmek de indirmeye dönüşür
  const browser = await chromium.launch({ channel: 'chromium' });
  console.log(`chromium ${browser.version()}`);
  const page = await browser.newPage({ acceptDownloads: true });

  for (const name of ['Download invoice (PDF)', 'Invoice from files host', 'Invoice from files host (attachment)']) {
    await page.goto('http://127.0.0.1:52113/book.html');
    const download = page.waitForEvent('download', { timeout: 3000 }).catch(() => null);
    await page.getByRole('link', { name, exact: true }).click();
    const file = await download;
    console.log(`${name.padEnd(38)} -> ${file ? `download "${file.suggestedFilename()}"` : `navigation ${page.url()}`}`);
  }
  await browser.close();
  servers.forEach(s => s.close());
})();
