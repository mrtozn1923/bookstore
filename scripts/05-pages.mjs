import { open } from './browser.mjs';

const catalog = () => {
    const r = s => document.querySelector(s).getBoundingClientRect();
    const lines = [`header: logo x=${r('.logo').left}, search ${r('.site-search').left.toFixed(1)}-` +
        `${r('.site-search').right.toFixed(1)}, cart right edge ${r('.cart-link').right} of ${document.documentElement.clientWidth}`];
    for (const card of document.querySelectorAll('.book-card')) {
        const c = card.getBoundingClientRect();
        const price = card.querySelector('.price').getBoundingClientRect();
        lines.push(`${card.querySelector('.book-title').textContent.padEnd(11)} x=${c.left.toFixed(1).padEnd(6)} ` +
            `y=${String(Math.round(c.top)).padEnd(4)} w=${c.width.toFixed(1).padEnd(6)} h=${c.height.toFixed(1).padEnd(6)} ` +
            `price bottom gap ${c.bottom - price.bottom}px`);
    }
    return lines.join('\n');
};

for (const engine of ['chromium', 'webkit']) {
    console.log(`--- ${engine}`);

    // Header and cards on a 1024px and a 768px catalog page
    for (const width of [1024, 768]) {
        const { browser, page } = await open('catalog.html', { engine, width });
        console.log(`${width}px ${await page.evaluate(catalog)}`);
        await browser.close();
    }

    // Coupon field on a 320px phone
    const { browser, page } = await open('checkout.html', { engine, width: 320 });
    console.log(await page.evaluate(() => {
        const r = s => document.querySelector(s).getBoundingClientRect();
        return `coupon group ${r('.input-group').width}px: input ${r('#coupon').width.toFixed(1)}px, ` +
            `button ${r('.input-group button').width.toFixed(1)}px, button right edge ${r('.input-group button').right}`;
    }));
    await browser.close();
}
