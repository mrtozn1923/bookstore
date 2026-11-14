import { open, engines } from './browser.mjs';
import { pixels } from './pixels.mjs';

for (const engine of engines) {
    console.log(`--- ${engine}`);
    const { browser, page } = await open('checkout.html', { engine });
    const express = page.locator('.shipping-option').nth(1);
    const textBox = () => express.locator('.cost').boundingBox();
    const before = await textBox();
    await express.click();
    const after = await textBox();
    console.log(`express selected: box-shadow ${await express.evaluate(el => getComputedStyle(el).boxShadow)}`);
    console.log(`price moved: ${before.x !== after.x || before.y !== after.y}`);
    await page.locator('#name').click();
    await page.keyboard.press('Tab');
    console.log(`#email after Tab: ${await page.locator('#email').evaluate(el => {
        const cs = getComputedStyle(el);
        return `:focus-visible=${el.matches(':focus-visible')} outline ${cs.outlineWidth} ${cs.outlineStyle} ${cs.outlineColor}, offset ${cs.outlineOffset}`;
    })}`);
    await browser.close();

    const catalog = await open('catalog.html', { engine });
    const card = catalog.page.locator('.book-card.featured');
    const box = await card.boundingBox();
    const { data } = await pixels(catalog.page, { x: box.x, y: box.y, width: 1, height: 1 });
    console.log(`featured card corner pixel: rgb(${data[0]}, ${data[1]}, ${data[2]}) (page background is rgb(249, 250, 251))`);
    const shadow = () => card.evaluate(el => getComputedStyle(el).boxShadow);
    console.log(`card shadow:       ${await shadow()}`);
    await card.hover();
    console.log(`card shadow hover: ${await shadow()}`);
    console.log(`box moved on hover: ${JSON.stringify(await card.boundingBox()) !== JSON.stringify(box)}`);
    await catalog.browser.close();
}

const { browser, page } = await open('checkout.html', { engine: 'chromium', forcedColors: 'active' });
await page.locator('#name').click();
await page.keyboard.press('Tab');
console.log('--- chromium, forced colors');
for (const [i, label] of [[0, 'selected  '], [1, 'unselected']]) {
    console.log(`${label} option: ${await page.locator('.shipping-option').nth(i).evaluate(el =>
        `box-shadow ${getComputedStyle(el).boxShadow}, border-color ${getComputedStyle(el).borderTopColor}`)}`);
}
console.log(`#email focus outline: ${await page.locator('#email')
    .evaluate(el => `${getComputedStyle(el).outlineStyle} ${getComputedStyle(el).outlineWidth}`)}`);
await browser.close();
