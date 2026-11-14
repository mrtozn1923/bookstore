import { open, engines } from './browser.mjs';
import { pixels } from './pixels.mjs';

const size = b => `${Math.round(b.width * 100) / 100}x${Math.round(b.height * 100) / 100}`;

for (const engine of engines) {
    console.log(`--- ${engine}`);
    // catalog: the cover grows inside its frame, the card does not move
    const catalog = await open('catalog.html', { engine });
    const card = catalog.page.locator('.book-card').nth(1);
    const before = { card: await card.boundingBox(), img: await card.locator('img').boundingBox() };
    await card.hover();
    await catalog.page.waitForTimeout(400);
    const after = { card: await card.boundingBox(), img: await card.locator('img').boundingBox(),
        frame: await card.locator('.cover').boundingBox() };
    console.log(`cover img scale: ${await card.locator('img').evaluate(el => getComputedStyle(el).scale)}`);
    console.log(`img ${size(before.img)} -> ${size(after.img)}, frame ${size(after.frame)}, card moved: `
        + `${JSON.stringify(before.card) !== JSON.stringify(after.card)}`);
    await catalog.browser.close();

    // book page: the cart icon swings from its bottom edge and keeps the hover scale
    const { browser, page } = await open('book.html', { engine });
    const icon = page.locator('.cart-icon');
    await page.locator('.add-to-cart').click();
    console.log(`cart count: ${await page.locator('.cart-count').textContent()}`);
    // freeze the swing at 25%, then hover the cart link
    await icon.evaluate(el => {
        const animation = el.getAnimations()[0];
        animation.pause();
        animation.currentTime = 100;
    });
    await page.locator('.cart-link').hover();
    console.log(`cart icon at 25% + hover: ${await icon.evaluate(el => {
        const cs = getComputedStyle(el);
        return `rotate ${cs.rotate}, scale ${cs.scale}, transform-origin ${cs.transformOrigin}`;
    })}`);

    // front and back cover
    const flip = page.locator('.cover-flip');
    const center = async () => {
        const b = await flip.boundingBox();
        const { data } = await pixels(page, { x: b.x + 120, y: b.y + 250, width: 1, height: 1 });
        return `rgb(${data[0]}, ${data[1]}, ${data[2]})`;
    };
    const front = engine === 'chromium' ? await center() : '-';
    await page.locator('.flip-button').click();
    await page.waitForTimeout(600);
    console.log(`flip: aria-pressed=${await page.locator('.flip-button').getAttribute('aria-pressed')}, `
        + `rotate ${await page.locator('.cover-flip-inner').evaluate(el => getComputedStyle(el).rotate)}, `
        + `cover box ${size(await flip.boundingBox())}`);
    if (engine === 'chromium') console.log(`cover pixel: front ${front}, after flip ${await center()}`);
    await browser.close();
}
