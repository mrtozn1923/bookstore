import { open, engines } from './browser.mjs';
import { pixels } from './pixels.mjs';

const rgb = d => `rgb(${d[0]}, ${d[1]}, ${d[2]})`;
async function colorAt(page, selector, x, y) {
    const box = await page.locator(selector).boundingBox();
    return rgb((await pixels(page, { x: box.x + x, y: box.y + y, width: 1, height: 1 })).data);
}
const size = async (page, selector) => {
    const b = await page.locator(selector).boundingBox();
    return `${Math.round(b.width * 100) / 100}x${Math.round(b.height * 100) / 100}`;
};

for (const engine of engines) {
    const { browser, page } = await open('lab/09-images.html', { engine });
    console.log(`--- ${engine}`);
    // 1) the frame around a 100x100 image
    for (const id of ['inline-img', 'block-img']) console.log(`${id.padEnd(10)} frame ${await size(page, `#${id}`)}`);
    // 2) a 1200x400 image in a 300px frame; then one whose file never arrives
    for (const id of ['max-only', 'reset']) console.log(`${id.padEnd(10)} img ${await size(page, `#${id} img`)}`);
    await page.route('**/never/*', () => {});
    await page.locator('#pending img').evaluate(img => { img.src = '../never/banner.webp'; });
    console.log(`pending    img ${await size(page, '#pending img')} before the file arrives`);
    // 3) a point near the top of the 96x96 crop
    for (const id of ['focus-center', 'focus-top']) {
        const pos = await page.locator(`#${id}`).evaluate(el => getComputedStyle(el).objectPosition);
        console.log(`${id.padEnd(12)} object-position ${pos.padEnd(8)} pixel (48, 12): ${await colorAt(page, `#${id}`, 48, 12)}`);
    }
    // 4) distinct colors in the scaled-up 8x8 image
    for (const id of ['code-smooth', 'code-pixelated']) {
        const b = await page.locator(`#${id}`).boundingBox();
        const { data } = await pixels(page, { x: Math.round(b.x), y: Math.round(b.y), width: 64, height: 64 });
        const colors = new Set();
        for (let i = 0; i < data.length; i += 4) colors.add(`${data[i]},${data[i + 1]},${data[i + 2]}`);
        console.log(`${id.padEnd(14)} distinct colors: ${colors.size}`);
    }
    // 4b) EXIF orientation: the file is 200x100 with a red band on its left
    for (const id of ['photo-auto', 'photo-none']) {
        const orientation = await page.locator(`#${id}`).evaluate(el => getComputedStyle(el).imageOrientation);
        const b = await page.locator(`#${id}`).boundingBox();
        console.log(`${id.padEnd(10)} image-orientation ${orientation.padEnd(10)} box ${await size(page, `#${id}`)}`
            + `  bottom left pixel ${await colorAt(page, `#${id}`, 5, b.height - 5)}`);
    }
    // 6) icon colors at the middle of the heart
    for (const id of ['icon-use', 'icon-fixed', 'icon-img']) {
        console.log(`${id.padEnd(10)} size ${await size(page, `#${id}`)}  pixel: ${await colorAt(page, `#${id}`, 10, 10)}`);
    }
    await browser.close();
}

// 7) the logo in a light and in a dark color scheme
for (const colorScheme of ['light', 'dark']) {
    const { browser, page } = await open('lab/09-images.html', { engine: 'chromium', colorScheme });
    console.log(`--- chromium, ${colorScheme}: logo ${await colorAt(page, '#logo', 24, 24)}, inverted logo ${await colorAt(page, '#logo-inverted', 24, 24)}`);
    await browser.close();
}
