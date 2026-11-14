import { open } from './browser.mjs';

for (const engine of ['chromium', 'webkit']) {
    const { browser, page } = await open('catalog.html', { engine, width: 1024 });
    console.log(`--- ${engine} 1024px`);
    console.log(await page.evaluate(() => {
        const list = document.querySelector('.book-list').getBoundingClientRect();
        const lines = [`book-list rows: ${getComputedStyle(document.querySelector('.book-list')).gridTemplateRows}`];
        for (const card of document.querySelectorAll('.book-card')) {
            const y = s => String(Math.round(card.querySelector(s).getBoundingClientRect().top - list.top)).padEnd(5);
            lines.push(`${card.querySelector('.book-title').textContent.padEnd(12)}` +
                `title y=${y('.book-title')}author y=${y('.book-author')}price y=${y('.price')}` +
                `rows: ${getComputedStyle(card).gridTemplateRows}`);
        }
        return lines.join('\n');
    }));
    await browser.close();
}
