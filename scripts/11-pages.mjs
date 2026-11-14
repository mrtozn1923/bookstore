import { open, engines } from './browser.mjs';

// a phone-sized viewport: the strip and the catalog both scroll
const phone = { width: 480, height: 800 };

for (const engine of engines) {
    console.log(`--- ${engine}`);
    // the Recommended strip on the book page
    const book = await open('book.html', { engine, ...phone });
    const strip = book.page.locator('.book-strip');
    const sizes = () => strip.evaluate(el =>
        `cover ${el.querySelector('img').getBoundingClientRect().width}px, scrollWidth ${el.scrollWidth}, clientWidth ${el.clientWidth}`);
    const shrink = await book.page.addStyleTag({ content: '.book-strip li { flex: initial; }' });
    console.log(`strip without flex: none: ${await sizes()}`);
    await shrink.evaluate(el => el.remove());
    console.log(`strip with flex: none:    ${await sizes()}`);
    console.log(`strip: ${await strip.evaluate(el => {
        const cs = getComputedStyle(el);
        return `scroll-snap-type ${cs.scrollSnapType}, scroll-padding-inline ${cs.scrollPaddingInlineStart}, overscroll-behavior-x ${cs.overscrollBehaviorX}`;
    })}`);
    await strip.evaluate(el => el.scrollTo({ left: 100, behavior: 'instant' }));
    await book.page.waitForTimeout(500);
    console.log(`strip: scrollTo(100) ends at ${await strip.evaluate(el => el.scrollLeft)}`);
    await strip.evaluate(el => el.scrollTo({ left: 0, behavior: 'instant' }));
    await book.page.locator('.book-strip a').nth(5).focus();
    await book.page.waitForTimeout(500);
    const focused = await strip.evaluate(el => {
        const a = el.querySelectorAll('a')[5].getBoundingClientRect();
        return `scrollLeft ${el.scrollLeft} (max ${el.scrollWidth - el.clientWidth}), cover starts ${Math.round(a.left - el.getBoundingClientRect().left)}px from the strip edge`;
    });
    console.log(`strip: focus on the 6th cover → ${focused}`);
    await book.browser.close();

    // the "Reading list" link on the catalog: where does the heading land, does the jump animate?
    for (const reducedMotion of ['no-preference', 'reduce']) {
        const { browser, page } = await open('catalog.html', { engine, reducedMotion, ...phone });
        // the reading list is the last section: one screen of space below it, so it can reach the top
        await page.addStyleTag({ content: 'main { padding-block-end: 100vh; }' });
        await page.click('a[href="#reading-list"]');
        const early = await page.evaluate(() => new Promise(r => requestAnimationFrame(() => r(window.scrollY))));
        await page.waitForTimeout(1500);
        const result = await page.evaluate(() => {
            const bar = document.querySelector('.filter-bar').getBoundingClientRect();
            const h2 = document.querySelector('#reading-list h2').getBoundingClientRect();
            return { y: window.scrollY, bar: Math.round(bar.bottom), h2: Math.round(h2.top) };
        });
        console.log(`reading list (${reducedMotion.padEnd(13)}): ${early < result.y ? 'animated' : 'jumped'}, `
            + `filter bar bottom ${result.bar}px, heading top ${result.h2}px`);
        await browser.close();
    }

    // the cart drawer: does the page shift or scroll behind it?
    const variants = {
        'as shipped': '',
        'no gutter': 'html { scrollbar-gutter: auto; }',
        'no lock': 'html:has(.cart-drawer[open]) { overflow: auto; }',
    };
    for (const [name, css] of Object.entries(variants)) {
        const { browser, page } = await open('catalog.html', { engine, scrollbars: true, ...phone });
        if (css) await page.addStyleTag({ content: css });
        const width = () => page.evaluate(() => Math.round(document.querySelector('main').getBoundingClientRect().width));
        const before = await width();
        await page.click('.cart-link');
        const after = await width();
        await page.mouse.move(40, 400); // left of the drawer: over the backdrop
        await page.mouse.wheel(0, 400);
        await page.waitForTimeout(800);
        const list = await page.locator('.cart-items').evaluate(el =>
            `list ${el.clientHeight}px of ${el.scrollHeight}px, overscroll-behavior ${getComputedStyle(el).overscrollBehaviorY}`);
        console.log(`drawer (${name.padEnd(10)}): main ${before}px → ${after}px, scrollY after wheel on backdrop ${await page.evaluate(() => window.scrollY)}, ${list}`);
        await browser.close();
    }
}
