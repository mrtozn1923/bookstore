import { open } from './browser.mjs';

// top edge of each card's rows, relative to the shelf
const rows = id => [...document.querySelectorAll(`#${id} .card`)].map(card => {
    const top = s => Math.round(card.querySelector(s).getBoundingClientRect().top -
        document.getElementById(id).getBoundingClientRect().top);
    return `  ${card.querySelector('h2').textContent.slice(0, 18).padEnd(19)} author y=${String(top('.author')).padEnd(4)}` +
        `blurb y=${String(top('.blurb')).padEnd(4)}price y=${top('.price')}`;
}).join('\n');

// position and size of every child, relative to its container
const boxes = id => {
    const box = document.getElementById(id);
    const b = box.getBoundingClientRect();
    return `#${id} (display: ${getComputedStyle(box).display})\n` + [...box.children].map(el => {
        const r = el.getBoundingClientRect();
        return `  ${el.textContent.slice(0, 20).padEnd(21)} x=${String(Math.round(r.left - b.left)).padEnd(4)}` +
            `y=${String(Math.round(r.top - b.top)).padEnd(4)}w=${Math.round(r.width)}`;
    }).join('\n');
};

for (const engine of ['chromium', 'webkit']) {
    console.log(`--- ${engine}`);
    let { browser, page } = await open('lab/07-subgrid.html', { engine });
    for (const id of ['without', 'with']) {
        console.log(`#${id}\n${await page.evaluate(rows, id)}`);
    }
    await browser.close();

    ({ browser, page } = await open('lab/07-align.html', { engine }));
    for (const id of ['steps', 'empty-cart', 'chips-flex', 'chips-grid', 'covers']) {
        console.log(await page.evaluate(boxes, id));
    }
    await browser.close();
}
