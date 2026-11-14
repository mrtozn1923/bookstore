import { open, engines } from './browser.mjs';
import { countColor } from './pixels.mjs';

for (const engine of engines) {
    const { browser, page } = await open('lab/13-table.html', { engine });
    console.log(`--- ${engine}`);
    // 1) the space between two cells, and the table width
    for (const id of ['separate', 'collapse']) {
        const r = await page.locator(`#${id}`).evaluate(t => {
            const [a, b] = [...t.querySelectorAll('td')].map(td => td.getBoundingClientRect());
            return `gap between cells ${Math.round((b.left - a.right) * 10) / 10}px, table ${t.getBoundingClientRect().width}px`;
        });
        console.log(`${id.padEnd(9)} ${r}`);
    }
    // 2) who decides the column widths?
    for (const id of ['auto', 'fixed']) {
        const r = await page.locator(`#${id}`).evaluate(t => {
            const cells = [...t.querySelectorAll('td')].map(td => Math.round(td.getBoundingClientRect().width));
            return `table ${Math.round(t.getBoundingClientRect().width)}px, columns ${cells.join(' + ')}, row height ${Math.round(t.rows[0].getBoundingClientRect().height)}px`;
        });
        console.log(`table-layout: ${id.padEnd(5)} ${r}`);
    }
    // 3) the width of two amounts
    const w = id => page.locator(`#${id}`).evaluate(el => el.getBoundingClientRect().width.toFixed(2));
    console.log(`system-ui:              ₺1,111.10 ${await w('sys-1')}px, ₺8,888.80 ${await w('sys-8')}px`);
    console.log(`system-ui tabular-nums: ₺1,111.10 ${await w('sys-tab-1')}px, ₺8,888.80 ${await w('sys-tab-8')}px`);
    console.log(`Georgia tabular-nums:   ₺1,111.10 ${await w('serif-tab-1')}px, ₺8,888.80 ${await w('serif-tab-8')}px`);
    // 4) scroll the box: is the header still on top, and is its red line still drawn?
    for (const id of ['sticky-separate', 'sticky-collapse']) {
        const box = page.locator(`#${id}`);
        await box.evaluate(el => { el.scrollTop = 120; });
        const b = await box.boundingBox();
        const th = await box.evaluate(el => Math.round(el.querySelector('th').getBoundingClientRect().top - el.getBoundingClientRect().top));
        const red = await countColor(page, { x: b.x, y: b.y, width: 300, height: 60 }, [185, 28, 28]);
        console.log(`${id.padEnd(15)} header ${th}px from the top of the box, red line pixels in the top 60px: ${red}`);
    }
    await browser.close();
}
