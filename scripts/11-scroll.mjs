import { open, engines } from './browser.mjs';
import { pixels } from './pixels.mjs';

// the two most common colors in an area (track and thumb)
async function mainColors(page, clip) {
    const { data } = await pixels(page, clip);
    const seen = new Map();
    for (let i = 0; i < data.length; i += 4) {
        const key = `rgb(${data[i]}, ${data[i + 1]}, ${data[i + 2]})`;
        seen.set(key, (seen.get(key) ?? 0) + 1);
    }
    return [...seen].sort((a, b) => b[1] - a[1]).slice(0, 2).map(([color]) => color).join(' + ');
}

for (const engine of engines) {
    // scrollbars: true → Chromium paints classic scrollbars, as on Windows
    const { browser, page } = await open('lab/11-overflow.html', { engine, scrollbars: true });
    console.log(`--- ${engine}`);
    // 1) focus a link that sits outside the box, then try to scroll the box from JavaScript
    for (const id of ['hidden', 'clip']) {
        await page.focus(`#${id}-link`);
        const afterFocus = await page.locator(`#${id}-box`).evaluate(el => el.scrollLeft);
        const afterScript = await page.locator(`#${id}-box`).evaluate(el => { el.scrollLeft = 0; el.scrollLeft = 30; return el.scrollLeft; });
        console.log(`overflow: ${id.padEnd(6)} scrollLeft after focus ${afterFocus}, after scrollLeft = 30: ${afterScript}`);
    }
    // 2) scrollIntoView under a 50px sticky header
    for (const id of ['target-plain', 'target-margin']) {
        const top = await page.locator(`#${id}`).evaluate(el => {
            el.scrollIntoView({ block: 'start', behavior: 'instant' });
            const feed = el.parentElement;
            return Math.round(el.getBoundingClientRect().top - feed.getBoundingClientRect().top - feed.clientTop);
        });
        console.log(`${id.padEnd(13)} lands ${top}px below the top of the feed (header is 50px)`);
    }
    // 3) how much width does the scrollbar take, and what color is it?
    for (const id of ['auto-short', 'auto-long', 'gutter-short', 'thin-long', 'color-long', 'legacy', 'both']) {
        const box = page.locator(`#${id}`);
        const { gutter, right, top } = await box.evaluate(el => {
            const r = el.getBoundingClientRect();
            return { gutter: el.offsetWidth - el.clientWidth - 2, right: r.right, top: r.top + window.scrollY };
        });
        const color = gutter > 0 ? await mainColors(page, { x: right - 1 - gutter, y: top + 4, width: gutter, height: 70 }) : '-';
        console.log(`${id.padEnd(13)} scrollbar ${String(gutter).padStart(2)}px  ${color}`);
    }
    // 4) wheel over the panel after it reached its end: does the page scroll too?
    for (const variant of ['long', 'long contain', 'short', 'short contain']) {
        await page.evaluate(v => {
            window.scrollTo(0, 0);
            const panel = document.querySelector('#panel');
            panel.className = v;
            panel.scrollTop = panel.scrollHeight;
        }, variant);
        const b = await page.locator('#panel').boundingBox();
        await page.mouse.move(b.x + 150, b.y + 150);
        await page.mouse.wheel(0, 300);
        await page.waitForTimeout(800);
        console.log(`panel ${variant.padEnd(13)} page scrollY after wheel: ${await page.evaluate(() => window.scrollY)}`);
    }
    await browser.close();
}

// 5) Chromium only: touch swipes through the DevTools protocol (WebKit has no such API in Playwright)
const { browser, page } = await open('lab/11-overflow.html', { hasTouch: true, isMobile: true });
console.log('--- chromium, touch');
// one swipe on a freshly loaded page; returns [strip scrollLeft, page scrollY]
async function swipeOnce(touchAction, xDistance, yDistance) {
    await page.reload();
    await page.evaluate(() => window.scrollTo(0, 0)); // reload restores the old scroll position
    await page.locator('#swipe').evaluate((el, t) => { el.style.touchAction = t; }, touchAction);
    const b = await page.locator('#swipe').boundingBox();
    const cdp = await page.context().newCDPSession(page);
    await cdp.send('Input.synthesizeScrollGesture',
        { x: b.x + 300, y: b.y + 40, xDistance, yDistance, gestureSourceType: 'touch', speed: 800 });
    await cdp.detach();
    await page.waitForTimeout(300);
    return page.evaluate(() => [document.querySelector('#swipe').scrollLeft, window.scrollY]);
}
for (const touchAction of ['auto', 'pan-y', 'pan-x', 'manipulation']) {
    const [strip] = await swipeOnce(touchAction, -200, 0); // finger moves to the left
    const [, pageY] = await swipeOnce(touchAction, 0, -200); // finger moves up
    console.log(`touch-action: ${touchAction.padEnd(12)} sideways swipe moves the strip: ${strip > 0 ? 'yes' : 'no '}, `
        + `upward swipe moves the page: ${pageY > 0 ? 'yes' : 'no'}`);
}
await browser.close();
