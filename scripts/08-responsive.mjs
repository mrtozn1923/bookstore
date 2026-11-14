import { open } from './browser.mjs';

for (const engine of ['chromium', 'webkit']) {
    console.log(`--- ${engine}`);

    // 1) rem inside a media query, and the range syntax
    for (const width of [760, 800, 1100]) {
        const { browser, page } = await open('lab/08-media-units.html', { engine, width });
        console.log(`${width}px: root font-size ${await page.evaluate(() => getComputedStyle(document.documentElement).fontSize)}, ` +
            `layout "${await page.evaluate(() => {
                const p = document.querySelector('.layout');
                const part = s => getComputedStyle(p, s).content.replace(/^none$/, '""').slice(1, -1);
                return part('::before') + part('::after');
            })}"`);
        await browser.close();
    }

    // 2) fluid type at three widths, then 200% and 300% zoom on a 1280px screen
    let base = null;
    for (const [width, zoom] of [[360, 1], [768, 1], [1280, 1], [1280, 2], [1280, 3]]) {
        // zoom: the layout viewport shrinks by the zoom factor, every CSS px covers zoom × device px
        const { browser, page } = await open('lab/08-fluid.html', { engine, width: Math.round(width / zoom), deviceScaleFactor: zoom });
        const sizes = await page.evaluate(() => ['.vw-only', '.fluid', '.body-text']
            .map(s => parseFloat(getComputedStyle(document.querySelector(s)).fontSize)));
        const screen = sizes.map(px => px * zoom);
        if (width === 1280 && zoom === 1) base = screen;
        const grow = base && zoom > 1 ? `  growth ${screen.map((px, i) => (px / base[i]).toFixed(2) + 'x').join(' / ')}` : '';
        console.log(`${String(width).padStart(4)}px zoom ${zoom * 100}%: on screen vw-only ${screen[0].toFixed(1)}px, ` +
            `fluid ${screen[1].toFixed(1)}px, body ${screen[2].toFixed(1)}px${grow}`);
        await browser.close();
    }

    // 3) user preferences on the catalog page
    const prefs = () => {
        const root = getComputedStyle(document.documentElement);
        const link = document.querySelector('.filters a').getBoundingClientRect();
        return `scroll-behavior ${root.scrollBehavior}, --color-border ${root.getPropertyValue('--color-border')}, ` +
            `hover ${matchMedia('(hover: hover)').matches ? 'yes' : 'no'}, dark ${matchMedia('(prefers-color-scheme: dark)').matches ? 'yes' : 'no'}, ` +
            `filter link height ${link.height}px`;
    };
    for (const [label, options] of [
        ['default', {}],
        ['less motion, more contrast, dark', { reducedMotion: 'reduce', contrast: 'more', colorScheme: 'dark' }],
        ['phone (touch)', { width: 390, height: 844, isMobile: true, hasTouch: true }],
    ]) {
        const { browser, page } = await open('catalog.html', { engine, ...options });
        console.log(`${label.padEnd(33)} ${await page.evaluate(prefs)}`);
        await browser.close();
    }
}

// 4) the cover on the book page keeps its 2:3 ratio at every width
for (const engine of ['chromium', 'webkit']) {
    for (const width of [320, 1280]) {
        const { browser, page } = await open('book.html', { engine, width });
        console.log(`${engine} ${width}px cover ${await page.evaluate(() => {
            const r = document.querySelector('.book-cover').getBoundingClientRect();
            return `${r.width}x${r.height}`;
        })}`);
        await browser.close();
    }
}
