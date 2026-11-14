import { open, engines } from './browser.mjs';
import { pixels } from './pixels.mjs';

async function colorAt(page, selector, x, y) {
    const box = await page.locator(selector).boundingBox();
    const { data } = await pixels(page, { x: box.x + x, y: box.y + y, width: 1, height: 1 });
    return `rgb(${data[0]}, ${data[1]}, ${data[2]})`;
}

for (const engine of engines) {
    const { browser, page } = await open('lab/05-edges.html', { engine });
    console.log(`--- ${engine}`);

    // 1) where does the text start, and how tall is the box?
    for (const id of ['plain', 'thick', 'inset', 'outlined']) {
        const [box, text] = await Promise.all([
            page.locator(`#${id}`).boundingBox(), page.locator(`#${id} span`).boundingBox()]);
        console.log(`${id.padEnd(9)} box ${box.width}x${box.height}  text at x=${text.x - box.x}, y=${text.y - box.y}`);
    }
    // 2) the shorthand and border-image
    for (const id of ['frame', 'frame-reset']) {
        console.log(`${id.padEnd(12)} border-image-source: ${await page.locator(`#${id}`)
            .evaluate(el => getComputedStyle(el).borderImageSource.slice(0, 30))}`);
    }
    // 3) a point 20px from the left, 2px from the top
    for (const id of ['percent', 'length']) {
        const radius = await page.locator(`#${id}`).evaluate(el => getComputedStyle(el).borderTopLeftRadius);
        console.log(`${id.padEnd(8)} radius ${radius.padEnd(7)} pixel (20, 2): ${await colorAt(page, `#${id}`, 20, 2)}`);
    }
    // 4) the outline's outer corner vs its straight edge
    console.log(`outline  corner (-6, -6): ${await colorAt(page, '#ring', -6, -6)}`
        + `  edge (60, -6): ${await colorAt(page, '#ring', 60, -6)}`);
    // 5) gradient borders: the very corner and the middle of the top border
    for (const id of ['grad-image', 'grad-clip']) {
        console.log(`${id.padEnd(10)} corner (1, 1): ${await colorAt(page, `#${id}`, 1, 1)}`
            + `  top border (80, 1): ${await colorAt(page, `#${id}`, 80, 1)}`);
    }
    console.log(`card box-shadow: ${await page.locator('#card').evaluate(el => getComputedStyle(el).boxShadow)}`);

    // 6) :focus-visible after Tab and after a click
    const focusState = id => page.locator(`#${id}`).evaluate(el =>
        `:focus=${el.matches(':focus')} :focus-visible=${el.matches(':focus-visible')} outline=${getComputedStyle(el).outlineStyle}`);
    await page.locator('#card').click();
    await page.keyboard.press('Tab');
    console.log(`input  after Tab:   ${await focusState('field')}`);
    await page.locator('#button').click();
    console.log(`button after click: ${await focusState('button')}`);
    await browser.close();
}

// forced colors (Windows High Contrast): what survives?
const { browser, page } = await open('lab/05-edges.html', { engine: 'chromium', forcedColors: 'active' });
console.log('--- chromium, forced colors');
for (const id of ['shadow-ring', 'transparent-ring']) {
    await page.locator(`#${id}`).focus();
    await page.keyboard.press('Shift+Tab');
    await page.keyboard.press('Tab');
    console.log(`${id.padEnd(17)} ${await page.locator(`#${id}`).evaluate(el => {
        const cs = getComputedStyle(el);
        return `box-shadow: ${cs.boxShadow}  outline: ${cs.outlineStyle} ${cs.outlineColor}`;
    })}`);
}
await browser.close();
