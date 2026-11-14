import { open, engines } from './browser.mjs';
import { pixels } from './pixels.mjs';

// color of one pixel (in CSS px) inside an element
async function colorAt(page, selector, x, y) {
    const box = await page.locator(selector).boundingBox();
    const { data } = await pixels(page, { x: box.x + x, y: box.y + y, width: 1, height: 1 });
    return `rgb(${data[0]}, ${data[1]}, ${data[2]})`;
}

for (const engine of engines) {
    const { browser, page } = await open('lab/04-background.html', { engine });
    console.log(`--- ${engine}`);
    console.log(`.banner.sale background-color: ${await page.locator('#sale')
        .evaluate(el => getComputedStyle(el).backgroundColor)}`);
    for (const id of ['cover', 'contain']) {
        console.log(`${id.padEnd(8)} left edge ${await colorAt(page, `#${id}`, 5, 100)}`
            + `  center ${await colorAt(page, `#${id}`, 200, 100)}`);
    }
    for (const id of ['srgb', 'oklab', 'oklch']) {
        console.log(`${id.padEnd(8)} middle of the gradient: ${await colorAt(page, `#${id}`, 200, 20)}`);
    }
    console.log(`hint     at 20%: ${await colorAt(page, '#hint', 80, 20)}  at 50%: ${await colorAt(page, '#hint', 200, 20)}`);
    console.log(`hard     at 49%: ${await colorAt(page, '#hard', 196, 20)}  at 51%: ${await colorAt(page, '#hard', 204, 20)}`);
    // the ring: 3 o'clock is 25%, 300 degrees is 83.3% (inside 84%), 310 degrees is 86.1% (outside)
    const point = deg => [60 + 50 * Math.sin(deg * Math.PI / 180), 60 - 50 * Math.cos(deg * Math.PI / 180)];
    for (const deg of [90, 300, 310]) {
        console.log(`rating   ${String(deg).padStart(3)}deg: ${await colorAt(page, '#rating', ...point(deg))}`);
    }
    await browser.close();
}
