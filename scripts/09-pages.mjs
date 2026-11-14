import { open, engines } from './browser.mjs';
import { pixels } from './pixels.mjs';

const rgb = d => `rgb(${d[0]}, ${d[1]}, ${d[2]})`;
async function colorAt(page, locator, x, y) {
    const box = await locator.boundingBox();
    return rgb((await pixels(page, { x: box.x + x, y: box.y + y, width: 1, height: 1 })).data);
}

for (const engine of engines) {
    console.log(`--- ${engine}`);
    const { browser, page, requests } = await open('catalog.html', { engine });
    const card = page.locator('.book-card').nth(1);
    const [frame, img] = await Promise.all([card.locator('.cover').boundingBox(), card.locator('.cover img').boundingBox()]);
    console.log(`cover frame height ${frame.height}, img height ${img.height}`);
    const tag = await card.locator('.price .icon').boundingBox();
    console.log(`price icon ${tag.width}x${tag.height}, sprite requests: ${requests.filter(p => p.endsWith('icons.svg')).length}`);

    const fav = card.locator('.fav');
    const state = async label => {
        const icon = fav.locator('.icon');
        const style = await icon.evaluate(el => `fill ${getComputedStyle(el).fill}, stroke ${getComputedStyle(el).stroke}`);
        console.log(`heart ${label.padEnd(8)} ${style}, center pixel ${await colorAt(page, icon, 12, 12)}`);
    };
    await state('normal');
    await fav.hover();
    await state('hover');
    await fav.click();
    await page.mouse.move(0, 0);
    await state('pressed');
    console.log(`aria-pressed: ${await fav.getAttribute('aria-pressed')}`);
    await browser.close();

    const book = await open('book.html', { engine });
    const photo = book.page.locator('.author-photo');
    await photo.scrollIntoViewIfNeeded();
    await photo.evaluate(el => el.decode());
    const p = await photo.boundingBox();
    // the page has scrolled: read the pixel in viewport coordinates
    const { data } = await pixels(book.page, { x: p.x + 48, y: p.y + 12, width: 1, height: 1 }, false);
    console.log(`author photo object-position ${await photo.evaluate(el => getComputedStyle(el).objectPosition)}, `
        + `pixel (48, 12): ${rgb(data)}`);
    await book.browser.close();
}
