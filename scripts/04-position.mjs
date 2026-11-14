import { open } from './browser.mjs';

for (const engine of ['chromium', 'webkit']) {
    console.log(`--- ${engine}`);

    // 1) Sticky toolbar inside a <main> with overflow-x
    let { browser, page } = await open('lab/04-sticky.html', { engine, height: 600 });
    for (const overflowX of ['hidden', 'clip', 'visible']) {
        const top = await page.evaluate(value => {
            document.querySelector('.catalog').style.overflowX = value;
            window.scrollTo(0, 0);
            window.scrollTo(0, 600);
            return document.querySelector('.catalog-toolbar').getBoundingClientRect().top;
        }, overflowX);
        console.log(`overflow-x: ${overflowX.padEnd(8)} toolbar top after scrolling 600px: ${top}px`);
    }
    await browser.close();

    // 2) Which element is painted at the badge's position while it is under the sticky header?
    ({ browser, page } = await open('lab/04-stacking.html', { engine, height: 600 }));
    const variants = {
        'no stacking context': '',
        '.book-list isolation': '.book-list { isolation: isolate; }',
        '.book-card transform': '.book-card { transform: translateX(0); }',
        '.book-card opacity': '.book-card { opacity: 0.99; }'
    };
    for (const [name, css] of Object.entries(variants)) {
        const hit = await page.evaluate(extra => {
            document.getElementById('variant')?.remove();
            const style = document.createElement('style');
            style.id = 'variant';
            style.textContent = extra;
            document.head.append(style);
            window.scrollTo(0, 30);
            const badge = document.querySelector('.badge').getBoundingClientRect();
            const el = document.elementFromPoint(badge.left + badge.width / 2, badge.top + badge.height / 2);
            return `badge top ${badge.top}px, on top: ${el.className || el.tagName.toLowerCase()}`;
        }, css);
        console.log(`${name.padEnd(22)} ${hit}`);
    }

    // 3) Top layer: z-index cannot compete with a modal dialog
    const topLayer = await page.evaluate(() => {
        document.querySelector('.site-header').style.zIndex = '2147483647';
        const dialog = document.getElementById('quick-view');
        dialog.showModal();
        const r = dialog.getBoundingClientRect();
        const el = document.elementFromPoint(r.left + 5, r.top + 5);
        return `header z-index ${getComputedStyle(document.querySelector('.site-header')).zIndex}, ` +
            `on top: ${el.id || el.tagName.toLowerCase()}`;
    });
    console.log(`modal dialog           ${topLayer}`);
    await browser.close();
}
