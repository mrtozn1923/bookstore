import { open } from './browser.mjs';

// prints "column/row" of every child, read back from the browser's own layout
const cells = id => {
    const grid = document.getElementById(id);
    const g = grid.getBoundingClientRect();
    const cs = getComputedStyle(grid);
    const lines = [`#${id}: columns ${cs.gridTemplateColumns}`];
    for (const el of grid.children) {
        const r = el.getBoundingClientRect();
        lines.push(`  ${el.textContent.slice(0, 24).padEnd(25)} x=${String(Math.round(r.left - g.left)).padEnd(4)} ` +
            `y=${String(Math.round(r.top - g.top)).padEnd(4)} w=${r.width.toFixed(1)}`);
    }
    return lines.join('\n');
};

for (const engine of ['chromium', 'webkit']) {
    console.log(`--- ${engine}`);

    // 1) fr, auto-fill vs auto-fit, 1fr vs minmax(0, 1fr)
    let { browser, page } = await open('lab/06-tracks.html', { engine });
    for (const id of ['fr', 'fill', 'fit', 'downloads-fr', 'downloads-zero']) {
        console.log(await page.evaluate(cells, id));
    }
    await browser.close();

    // 2) Implicit rows, spanning items, named lines and dense packing
    ({ browser, page } = await open('lab/06-placement.html', { engine }));
    for (const id of ['sparse', 'dense']) {
        console.log(await page.evaluate(cells, id));
    }
    await browser.close();
}
