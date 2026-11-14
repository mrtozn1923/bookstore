import { open } from './browser.mjs';

for (const engine of ['chromium', 'webkit']) {
    console.log(`--- ${engine}`);

    // 1) How flex-grow and flex-shrink share the space
    let { browser, page } = await open('lab/05-flex-math.html', { engine });
    console.log(await page.evaluate(() => {
        const lines = [];
        for (const id of ['wide', 'narrow']) {
            const row = document.getElementById(id);
            const widths = [...row.children].map(el => `${el.className} ${el.getBoundingClientRect().width}`);
            lines.push(`${id.padEnd(6)} (${row.getBoundingClientRect().width}px): ${widths.join(', ')}`);
        }
        for (const el of document.querySelectorAll('#shorthands p')) {
            const cs = getComputedStyle(el);
            lines.push(`flex: ${el.textContent.padEnd(8)} -> grow ${cs.flexGrow}, shrink ${cs.flexShrink}, basis ${cs.flexBasis}`);
        }
        return lines.join('\n');
    }));
    await browser.close();

    // 2) A long title in a flex item: min-width auto vs 0
    ({ browser, page } = await open('lab/05-min-width.html', { engine }));
    console.log(await page.evaluate(() => ['broken', 'fixed'].map(id => {
        const line = document.getElementById(id);
        const details = line.querySelector('.details').getBoundingClientRect();
        const price = line.querySelector('.price').getBoundingClientRect();
        return `${id.padEnd(6)} details ${details.width.toFixed(1)}px, price right edge ${price.right.toFixed(1)}px, ` +
            `line right edge ${line.getBoundingClientRect().right}px`;
    }).join('\n')));
    await browser.close();

    // 3) order changes the picture, not the Tab sequence
    ({ browser, page } = await open('lab/05-order.html', { engine }));
    const visual = await page.$$eval('.card-actions > *', els =>
        els.sort((a, b) => a.getBoundingClientRect().left - b.getBoundingClientRect().left).map(e => e.textContent));
    console.log('visual order:', visual.join(' | '));
    if (engine === 'chromium') {
        const tabs = [];
        for (let i = 0; i < 3; i++) {
            await page.keyboard.press('Tab');
            tabs.push(await page.evaluate(() => document.activeElement.textContent));
        }
        console.log('Tab order:   ', tabs.join(' | '));
    }
    await browser.close();
}
