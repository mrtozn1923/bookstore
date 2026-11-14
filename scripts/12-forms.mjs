import { open, engines } from './browser.mjs';
import { pixels } from './pixels.mjs';

// the two most common colors in an element's box
async function colors(page, selector) {
    const { data } = await pixels(page, await page.locator(selector).boundingBox());
    const seen = new Map();
    for (let i = 0; i < data.length; i += 4) {
        const key = `rgb(${data[i]}, ${data[i + 1]}, ${data[i + 2]})`;
        seen.set(key, (seen.get(key) ?? 0) + 1);
    }
    return [...seen].sort((a, b) => b[1] - a[1]).slice(0, 2).map(([color]) => color).join(' + ');
}
// how many dark pixels (the check mark or the border) are inside the box?
async function darkPixels(page, selector) {
    const { data } = await pixels(page, await page.locator(selector).boundingBox());
    let count = 0;
    for (let i = 0; i < data.length; i += 4) if (data[i] + data[i + 1] + data[i + 2] < 150) count++;
    return count;
}

for (const engine of engines) {
    const { browser, page } = await open('lab/12-forms.html', { engine });
    console.log(`--- ${engine}`);
    // 1) and 2)
    const font = id => page.locator(id).evaluate(el => getComputedStyle(el).fontSize);
    console.log(`font-size: default input ${await font('#raw')}, font: inherit ${await font('#reset')}`);
    console.log(`color-scheme: dark input   ${await colors(page, '#dark-input')}`);
    // 3)
    for (const id of ['accent-check', 'accent-radio', 'light-check']) console.log(`${id.padEnd(27)} ${await colors(page, `#${id}`)}`);
    // 4) appearance: none
    for (const id of ['naive-on', 'naive-off', 'better-on', 'better-off']) console.log(`check ${id.padEnd(21)} ${await colors(page, `#${id}`)}`);
    await page.focus('#better-off');
    await page.keyboard.press('Space');
    console.log(`Space on #better-off: checked ${await page.locator('#better-off').isChecked()}`);
    // 5)
    console.log(`range thumb: ${await colors(page, '#range')}`);
    // 6) mouse vs keyboard
    await page.locator('#ghost').evaluate(el => { window.clicks = 0; el.addEventListener('click', () => window.clicks++); });
    const g = await page.locator('#ghost').boundingBox();
    await page.mouse.click(g.x + 10, g.y + 10);
    const byMouse = await page.evaluate(() => window.clicks);
    await page.focus('#ghost');
    await page.keyboard.press('Enter');
    console.log(`pointer-events: none button: clicks by mouse ${byMouse}, after focus + Enter ${await page.evaluate(() => window.clicks)}`);
    // 7) what does selecting the whole line copy?
    for (const id of ['plain', 'unprefixed', 'both']) {
        const text = await page.locator(`#${id}`).evaluate(el => {
            getSelection().selectAllChildren(el);
            return getSelection().toString();
        });
        console.log(`select #${id.padEnd(11)} "${text}"`);
    }
    await page.click('#code');
    console.log(`one click on #code selects "${await page.evaluate(() => getSelection().toString())}"`);
    console.log(`#unprefixed button: user-select ${await page.locator('#unprefixed button').first().evaluate(el => {
        const cs = getComputedStyle(el);
        return `${cs.userSelect}, -webkit-user-select ${cs.webkitUserSelect}`;
    })}`);
    // 8)
    await page.focus('#inert-button');
    console.log(`interactivity: inert supported ${await page.evaluate(() => CSS.supports('interactivity', 'inert'))}, `
        + `focus() on the button inside → active element ${await page.evaluate(() => document.activeElement.id || document.activeElement.tagName)}`);
    console.log(`cursor: ${await page.locator('#zoom').evaluate(el => getComputedStyle(el).cursor)}`);
    console.log(`appearance: base-select supported ${await page.evaluate(() => CSS.supports('appearance', 'base-select'))}`);
    await browser.close();
}

// forced colors (Windows high contrast) can only be emulated in Chromium
const { browser, page } = await open('lab/12-forms.html', { forcedColors: 'active' });
console.log('--- chromium, forced colors');
for (const id of ['naive-on', 'naive-off', 'better-on', 'better-off']) {
    console.log(`check ${id.padEnd(11)} dark pixels ${await darkPixels(page, `#${id}`)}, background ${await page.locator(`#${id}`).evaluate(el => getComputedStyle(el).backgroundColor)}`);
}
await browser.close();
