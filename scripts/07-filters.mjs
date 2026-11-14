import { open, engines } from './browser.mjs';
import { pixels } from './pixels.mjs';

const rgb = d => `rgb(${d[0]}, ${d[1]}, ${d[2]})`;
async function colorAt(page, selector, x, y) {
    const box = await page.locator(selector).boundingBox();
    return rgb((await pixels(page, { x: box.x + x, y: box.y + y, width: 1, height: 1 })).data);
}

for (const engine of engines) {
    const { browser, page } = await open('lab/07-filters.html', { engine });
    console.log(`--- ${engine}`);
    // 1) the red corner of the square after each filter chain
    for (const id of ['original', 'gray-sepia', 'sepia-gray']) {
        const filter = await page.locator(`#${id}`).evaluate(el => getComputedStyle(el).filter);
        console.log(`${id.padEnd(10)} filter ${filter.padEnd(24)} red corner: ${await colorAt(page, `#${id}`, 10, 10)}`);
    }
    // 2) red shadow pixels inside the image box and outside it (within the 10px offset)
    for (const id of ['box-shadow', 'drop-shadow']) {
        const b = await page.locator(`#${id}`).boundingBox();
        const { data, width } = await pixels(page, { x: b.x, y: b.y, width: b.width + 10, height: b.height + 10 });
        let inside = 0, outside = 0;
        for (let i = 0; i < data.length; i += 4) {
            const x = (i / 4) % width, y = Math.floor(i / 4 / width);
            const red = Math.abs(data[i] - 185) + Math.abs(data[i + 1] - 28) + Math.abs(data[i + 2] - 28) < 30;
            if (red) (x < b.width && y < b.height ? inside++ : outside++);
        }
        console.log(`${id.padEnd(11)} red shadow pixels inside the box: ${String(inside).padEnd(5)} outside: ${outside}`);
    }
    // 3) where does the filter go?
    for (const id of ['card-filter', 'img-filter']) {
        console.log(`${id.padEnd(11)} badge: ${await colorAt(page, `#${id} .badge`, 2, 2)}  cover: ${await colorAt(page, `#${id} img`, 10, 10)}`);
    }
    // 4) a row of 16 pixels in the middle of the bar (Chromium only: Playwright's WebKit screenshots skip backdrop-filter)
    for (const id of engine === 'chromium' ? ['plain', 'frosted'] : []) {
        const b = await page.locator(`#${id} .bar`).boundingBox();
        const { data } = await pixels(page, { x: b.x + 80, y: b.y + 20, width: 16, height: 1 });
        const reds = data.filter((_, i) => i % 4 === 0);
        console.log(`${id.padEnd(8)} backdrop-filter ${(await page.locator(`#${id} .bar`).evaluate(el => getComputedStyle(el).backdropFilter)).padEnd(9)}`
            + ` row min ${Math.min(...reds)}, max ${Math.max(...reds)}`);
    }
    // 5) duotone: black, middle gray and white of the gradient
    console.log(`duotone   left ${await colorAt(page, '#duotone', 1, 20)}  middle ${await colorAt(page, '#duotone', 100, 20)}`
        + `  right ${await colorAt(page, '#duotone', 198, 20)}`);
    // 6) the blue chip on the amber page
    for (const id of ['blended', 'isolated']) {
        console.log(`${id.padEnd(9)} chip: ${await colorAt(page, `#${id} .chip`, 30, 15)}`);
    }
    await browser.close();
}
