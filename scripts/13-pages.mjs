import { open, engines } from './browser.mjs';
import { countColor } from './pixels.mjs';

for (const engine of engines) {
    console.log(`--- ${engine}`);
    const { browser, page } = await open('orders.html', { engine });
    const table = page.locator('.orders');
    console.log(await table.evaluate(t => {
        const widths = [...t.rows[0].cells].map(c => Math.round(c.getBoundingClientRect().width));
        const caption = t.caption.getBoundingClientRect().top > t.tBodies[0].getBoundingClientRect().bottom ? 'below' : 'above';
        return `columns ${widths.join(' + ')}px, caption ${caption} the rows, row 2 background ${getComputedStyle(t.tBodies[0].rows[1]).backgroundColor}`;
    }));
    // right aligned totals of the first six rows: where does each amount start?
    const starts = () => table.evaluate(t => [...t.tBodies[0].rows].slice(0, 6).map(r => {
        const range = document.createRange();
        range.selectNodeContents(r.cells[4]);
        return `${r.cells[4].textContent} ${Math.round(range.getBoundingClientRect().left * 10) / 10}`;
    }).join(', '));
    console.log(`amount starts at x, tabular-nums: ${await starts()}`);
    const proportional = await page.addStyleTag({ content: '.data-table .num { font-variant-numeric: normal; }' });
    console.log(`amount starts at x, proportional: ${await starts()}`);
    await proportional.evaluate(el => el.remove());

    // sticky header: scroll the table's own box
    const box = page.locator('.table-scroll');
    await box.evaluate(el => { el.scrollTop = 400; });
    const b = await box.boundingBox();
    const head = await box.evaluate(el => {
        const th = el.querySelector('th').getBoundingClientRect();
        return { top: Math.round(th.top - el.getBoundingClientRect().top - el.clientTop), bottom: th.bottom };
    });
    const line = await countColor(page, { x: b.x + 1, y: head.bottom - 2, width: b.width - 2, height: 2 }, [209, 213, 219], 6);
    console.log(`table box scrolled 400px: header ${head.top}px from the box top, line under the header ${line} px`);

    // the scroll box from HTML5 post 10 (only overflow-x: auto): the page scrolls, the header does not stick
    await box.evaluate(el => { el.scrollTop = 0; });
    await page.addStyleTag({ content: '.table-scroll { max-block-size: none; overflow-x: auto; overflow-y: visible; }' });
    await page.evaluate(() => window.scrollTo({ top: 600, behavior: 'instant' }));
    console.log(`overflow-x: auto box, page scrolled 600px: header top ${await page.locator('.orders th').first().evaluate(th => Math.round(th.getBoundingClientRect().top))}px in the viewport, `
        + `computed overflow-y ${await box.evaluate(el => getComputedStyle(el).overflowY)}`);
    await browser.close();
}
