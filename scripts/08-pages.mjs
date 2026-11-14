import { open, engines } from './browser.mjs';
import { pixels } from './pixels.mjs';

const rgb = d => `rgb(${d[0]}, ${d[1]}, ${d[2]})`;
async function colorAt(page, locator, x, y) {
    const box = await locator.boundingBox();
    return rgb((await pixels(page, { x: box.x + x, y: box.y + y, width: 1, height: 1 })).data);
}

for (const engine of engines) {
    console.log(`--- ${engine}`);
    const { browser, page } = await open('book.html', { engine });
    // images are lazy: scroll through the page so that all of them load
    await page.evaluate(async () => {
        for (const img of document.images) { img.scrollIntoView(); await img.decode().catch(() => {}); }
        window.scrollTo(0, 0);
        document.querySelector('.book-strip').scrollLeft = 0;
    });

    const photo = page.locator('.author-photo');
    const box = await photo.boundingBox();
    console.log(`author photo: ${box.width}x${box.height}, clip-path ${await photo.evaluate(el => getComputedStyle(el).clipPath)}`);
    console.log(`  corner (3, 3): ${await colorAt(page, photo, 3, 3)}  center: ${await colorAt(page, photo, 48, 48)}`);

    const strip = page.locator('.book-strip');
    const sizes = await strip.evaluate(el => `scrollWidth ${el.scrollWidth}, clientWidth ${el.clientWidth}`);
    const s = await strip.boundingBox();
    console.log(`strip: ${sizes}`);
    console.log(`  mask-image ${await strip.evaluate(el => getComputedStyle(el).maskImage)}`);
    // the fourth cover is cut by the right edge: compare its color at the edge with its color 60px further in
    console.log(`  pixel 6px from the right edge: ${await colorAt(page, strip, s.width - 6, 100)}`
        + `  60px from the edge: ${await colorAt(page, strip, s.width - 60, 100)}`);

    const starts = await page.locator('.edition').evaluate(section => {
        const range = document.createRange();
        range.selectNodeContents(section.querySelector('p'));
        const left = section.getBoundingClientRect().left;
        const lines = new Map();
        for (const r of range.getClientRects()) {
            const y = Math.round(r.top);
            if (!lines.has(y) || r.left < lines.get(y)) lines.set(y, r.left);
        }
        return [...lines.values()].map(x => Math.round(x - left));
    });
    console.log(`edition text line starts: ${starts.join(', ')}`);
    await browser.close();
}
