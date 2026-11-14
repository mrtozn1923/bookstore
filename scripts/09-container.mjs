import { open } from './browser.mjs';

// layout of one card, relative to the card itself
const card = selector => {
    const el = document.querySelector(selector);
    const c = el.getBoundingClientRect();
    const r = s => el.querySelector(s).getBoundingClientRect();
    const cs = getComputedStyle(el);
    return `w=${Math.round(c.width)} cover ${Math.round(r('img').width)}px, title at x=${Math.round(r('.book-title').left - c.left)} ` +
        `y=${Math.round(r('.book-title').top - c.top)} size ${getComputedStyle(el.querySelector('.book-title')).fontSize}, columns "${cs.gridTemplateColumns}", background ${cs.backgroundColor}`;
};

for (const engine of ['chromium', 'webkit']) {
    console.log(`--- ${engine}`);

    // 1) the same card in three places on one page
    for (const width of [1280, 360]) {
        const { browser, page } = await open('catalog.html', { engine, width });
        console.log(`${width}px featured        ${await page.evaluate(card, '.featured .book-card')}`);
        console.log(`${width}px recently viewed ${await page.evaluate(card, '.recently-viewed .book-card')}`);
        console.log(`${width}px list            ${await page.evaluate(card, '.book-list .book-card')}`);
        await browser.close();
    }

    // 2) pitfalls: subgrid + container-type, self query, size containment
    const { browser, page } = await open('lab/09-pitfalls.html', { engine });
    console.log(await page.evaluate(() => {
        const shelf = document.getElementById('shelf').getBoundingClientRect();
        const lines = [...document.querySelectorAll('.card')].map(c =>
            `${c.querySelector('h2').textContent.slice(0, 11).padEnd(12)} author y=${String(Math.round(c.querySelector('.author')
                .getBoundingClientRect().top - shelf.top)).padEnd(4)} rows "${getComputedStyle(c).gridTemplateRows}"`);
        const panel = document.getElementById('panel');
        lines.push(`panel: outline ${getComputedStyle(panel).outlineStyle}, text weight ${getComputedStyle(panel.querySelector('p')).fontWeight}`);
        lines.push(`container-type: size, height ${document.getElementById('both').getBoundingClientRect().height}px`);
        return lines.join('\n');
    }));
    await browser.close();
}
