import { open } from './browser.mjs';

for (const engine of ['chromium', 'webkit']) {
    const { browser, page } = await open('lab/01-context.html', { engine });
    const colors = await page.$$eval('stock-badge', els => els.map(e =>
        `${(e.className || 'normal').padEnd(7)} ${getComputedStyle(e).color}`));
    console.log(engine, colors.join(' | '));
    await browser.close();
}
