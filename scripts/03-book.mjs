import { open } from './browser.mjs';

const measure = page => page.evaluate(() => {
    const $ = s => document.querySelector(s);
    const cover = $('.book-cover').getBoundingClientRect();
    const crumb = $('.breadcrumb');
    const summary = $('.book-summary');
    const price = getComputedStyle($('.price'));
    return [
        `cover: ${cover.width} x ${cover.height}`,
        `breadcrumb: client ${crumb.clientWidth}px, content ${crumb.scrollWidth}px`,
        `summary: ${summary.clientHeight}px tall (line-height ${getComputedStyle(summary).lineHeight}), ` +
            `max ${getComputedStyle(summary).maxWidth}`,
        `price border left/right: ${price.borderLeftWidth} / ${price.borderRightWidth}, ` +
            `padding left/right: ${price.paddingLeft} / ${price.paddingRight}`
    ].join('\n');
});

for (const engine of ['chromium', 'webkit']) {
    for (const width of [320, 1280]) {
        const { browser, page } = await open('book.html', { engine, width });
        console.log(`--- ${engine} ${width}px`);
        console.log(await measure(page));
        if (width === 1280) {
            // the same CSS in a right-to-left document
            await page.evaluate(() => { document.documentElement.dir = 'rtl'; });
            console.log('dir=rtl ->', (await measure(page)).split('\n').pop());
        }
        await browser.close();
    }
}
