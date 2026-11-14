import { open } from './browser.mjs';

for (const engine of ['chromium', 'webkit']) {
    console.log(`--- ${engine}`);
    for (const width of [1280, 1024, 768, 360]) {
        const { browser, page } = await open('catalog.html', { engine, width });
        console.log(`${width}px`, await page.evaluate(() => {
            const r = s => document.querySelector(s).getBoundingClientRect();
            const box = s => `x=${Math.round(r(s).left)} y=${Math.round(r(s).top)} w=${Math.round(r(s).width)}`;
            const list = getComputedStyle(document.querySelector('.book-list')).gridTemplateColumns;
            return `areas: filters ${box('.filters')} | main ${box('.catalog')}\n` +
                `  book-list columns: ${list}\n` +
                `  The Hobbit: ${box('.book-card:last-child')}`;
        }));
        await browser.close();
    }
}
