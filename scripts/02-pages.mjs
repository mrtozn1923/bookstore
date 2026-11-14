import { open, engines } from './browser.mjs';

for (const engine of engines) {
    const { browser, page } = await open('catalog.html', { engine });
    console.log(`--- ${engine}`);
    console.log(await page.evaluate(() => {
        const badge = document.querySelector('.badge');
        const price = document.querySelector('.price');
        const icon = price.querySelector('svg').getBoundingClientRect();

        // a zero-size inline-block sits on the baseline
        const probe = document.createElement('span');
        probe.style.cssText = 'display: inline-block; inline-size: 0; block-size: 0';
        price.append(probe);
        const baseline = probe.getBoundingClientRect().bottom;
        probe.remove();
        // the digits' ink height, from canvas text metrics in the same font
        const ctx = document.createElement('canvas').getContext('2d');
        ctx.font = getComputedStyle(price).font;
        const digitsCenter = baseline - ctx.measureText('420').actualBoundingBoxAscent / 2;

        return [
            `badge: font-size ${getComputedStyle(badge).fontSize}, letter-spacing ${getComputedStyle(badge).letterSpacing}`,
            `price icon center ${(icon.top + icon.height / 2 - baseline).toFixed(1)}px, `
                + `digits center ${(digitsCenter - baseline).toFixed(1)}px (from baseline)`,
            `initial-letter: ${CSS.supports('initial-letter: 2')}, -webkit-initial-letter: ${CSS.supports('-webkit-initial-letter: 2')}`,
        ].join('\n');
    }));
    const link = page.locator('.book-card h2 a').first();
    const before = await link.boundingBox();
    await link.hover();
    const after = await link.boundingBox();
    const style = await link.evaluate(el => {
        const cs = getComputedStyle(el);
        return `${cs.textDecorationThickness} ${cs.textDecorationColor}`;
    });
    console.log(`card link hover: ${style}, box moved: ${before.y !== after.y || before.height !== after.height}`);
    await browser.close();
}
