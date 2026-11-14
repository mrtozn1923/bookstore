import { open } from './browser.mjs';

for (const engine of ['chromium', 'webkit']) {
    const { browser, page } = await open('lab/01-keywords.html', { engine });
    console.log(`--- ${engine} ${browser.version()}`);
    const rows = await page.$$eval('.buy-box button', buttons => buttons.map(b => {
        const cs = getComputedStyle(b);
        return `${b.textContent.padEnd(13)} color: ${cs.color.padEnd(20)} background-color: ${cs.backgroundColor}`;
    }));
    rows.forEach(r => console.log(r));
    await browser.close();
}
