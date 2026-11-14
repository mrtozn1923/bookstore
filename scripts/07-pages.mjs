import { open, engines } from './browser.mjs';
import { pixels } from './pixels.mjs';

const rgb = d => `rgb(${d[0]}, ${d[1]}, ${d[2]})`;

for (const engine of engines) {
    console.log(`--- ${engine}`);
    // catalog: the sold-out card
    const { browser, page } = await open('catalog.html', { engine, width: 600, height: 500 });
    const soldOut = page.locator('.book-card.is-sold-out');
    console.log(`sold out cover: ${await soldOut.locator('img').evaluate(el =>
        `filter ${getComputedStyle(el).filter}, opacity ${getComputedStyle(el).opacity}`)}`);
    const badge = await soldOut.locator('.badge').boundingBox();
    console.log(`sold out badge pixel: ${rgb((await pixels(page, { x: badge.x + 2, y: badge.y + 2, width: 1, height: 1 })).data)}`);

    // the sticky filter bar after scrolling a cover under it
    await page.evaluate(() => window.scrollTo(0, 300));
    await page.waitForTimeout(200);
    const bar = page.locator('.filter-bar');
    const b = await bar.boundingBox();
    console.log(`filter bar after scroll: y=${b.y}, ${await bar.evaluate(el => {
        const cs = getComputedStyle(el);
        return `backdrop-filter ${cs.backdropFilter}, background ${cs.backgroundColor}`;
    })}`);
    if (engine === 'chromium') {
        // Playwright's WebKit screenshots skip backdrop-filter, so the pixel check runs in Chromium only
        const cover = await page.locator('.cover').nth(1).boundingBox();
        const { data } = await pixels(page, { x: cover.x + cover.width / 2, y: b.y + b.height / 2, width: 1, height: 1 }, false);
        console.log(`filter bar pixel over the cover: ${rgb(data)}`);
    }
    await browser.close();

    // home: red-dominant pixels in the banner with and without the blend modes
    for (const blend of ['normal', null]) {
        const home = await open('index.html', { engine });
        // without duotone = the post 04 banner: overlay + photo, no blend modes
        if (blend) await home.page.addStyleTag({ content: `.promo-banner { background-blend-mode: normal;
            background-image: linear-gradient(to top, rgb(15 23 42 / 0.9), rgb(15 23 42 / 0.3) 70%), url("img/banner-1x.webp"); }` });
        const box = await home.page.locator('.promo-banner').boundingBox();
        const { data } = await pixels(home.page, { x: box.x, y: box.y, width: box.width, height: 60 });
        let red = 0;
        for (let i = 0; i < data.length; i += 4) if (data[i] > data[i + 1] + 60 && data[i] > data[i + 2] + 60) red++;
        const mode = await home.page.locator('.promo-banner').evaluate(el => getComputedStyle(el).backgroundBlendMode);
        console.log(`banner ${(blend ? 'without duotone' : 'with duotone').padEnd(15)} red-dominant pixels in the top 60px: ${String(red).padEnd(5)} (${mode})`);
        await home.browser.close();
    }
}

// forced colors (Windows High Contrast): what is left of the banner?
const forced = await open('index.html', { engine: 'chromium', forcedColors: 'active' });
console.log('--- chromium, forced colors');
console.log(`banner: ${await forced.page.locator('.promo-banner').evaluate(el => {
    const cs = getComputedStyle(el);
    return `background-image ${cs.backgroundImage}, background-color ${cs.backgroundColor}, color ${cs.color}`;
})}`);
await forced.browser.close();
