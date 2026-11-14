import { open, engines } from './browser.mjs';
import { pixels } from './pixels.mjs';

const r = n => Math.round(n * 100) / 100;

for (const engine of engines) {
    const { browser, page } = await open('lab/06-transforms.html', { engine });
    console.log(`--- ${engine}`);
    // the box sits at (0, 80) in its stage, 80x40: its untransformed center is (40, 100)
    const moved = id => page.locator(`#${id}`).evaluate(el => {
        const b = el.getBoundingClientRect(), s = el.parentElement.getBoundingClientRect();
        return { cx: b.x - s.x + b.width / 2 - 40, cy: b.y - s.y + b.height / 2 - 100, w: b.width, h: b.height,
            x: b.x - s.x, y: b.y - s.y - 80 };
    });
    // 1) order of functions, 2) individual properties
    for (const id of ['translate-rotate', 'rotate-translate', 'individual', 'rotate-only', 'combined']) {
        const m = await moved(id);
        console.log(`${id.padEnd(17)} center moved (${r(m.cx)}, ${r(m.cy)})  bbox ${r(m.w)}x${r(m.h)}`);
    }
    console.log(`#individual computed: ${await page.locator('#individual').evaluate(el => {
        const cs = getComputedStyle(el);
        return `translate: ${cs.translate}; rotate: ${cs.rotate}; transform: ${cs.transform}`;
    })}`);
    console.log(`#combined computed:   ${await page.locator('#combined').evaluate(el =>
        `rotate: ${getComputedStyle(el).rotate}; transform: ${getComputedStyle(el).transform}`)}`);
    // 3) transform-origin: where does the rotated box's bounding box start?
    for (const id of ['origin-center', 'origin-corner']) {
        const m = await moved(id);
        const origin = await page.locator(`#${id}`).evaluate(el => getComputedStyle(el).transformOrigin);
        console.log(`${id.padEnd(14)} transform-origin ${origin.padEnd(9)} bbox from (${r(m.x)}, ${r(m.y)}) size ${r(m.w)}x${r(m.h)}`);
    }
    // 4) layout: the neighbour does not move, offsetWidth ignores the transform
    console.log(`#grown: offsetWidth ${await page.locator('#grown').evaluate(el => el.offsetWidth)}, `
        + `rendered width ${(await page.locator('#grown').boundingBox()).width}; `
        + `#after starts at x=${(await page.locator('#after').boundingBox()).x - (await page.locator('#before').boundingBox()).x}`);
    // 5) animations paused half way
    for (const id of ['anim-transform', 'anim-rotate']) {
        const out = await page.locator(`#${id}`).evaluate(el => {
            el.getAnimations()[0].currentTime = 500;
            const cs = getComputedStyle(el), b = el.getBoundingClientRect();
            return `transform: ${cs.transform}; scale: ${cs.scale}; rotate: ${cs.rotate}; bbox ${Math.round(b.width * 100) / 100}`;
        });
        console.log(`${id.padEnd(15)} ${out}`);
    }
    // 6) 3D flip: which face is at the center of the card?
    const names = { '21,128,61': 'front (green)', '30,41,59': 'back (navy)' };
    // pixels only in Chromium: Playwright's WebKit screenshots ignore backface-visibility
    for (const id of engine === 'chromium' ? ['flip-ok', 'flip-visible', 'flip-clipped'] : []) {
        const shows = async () => {
            const b = await page.locator(`#${id}`).boundingBox();
            const { data } = await pixels(page, { x: b.x + 60, y: b.y + 90, width: 1, height: 1 });
            return names[`${data[0]},${data[1]},${data[2]}`] ?? `${data[0]},${data[1]},${data[2]}`;
        };
        const before = await shows();
        await page.locator(`#${id}`).evaluate(el => el.classList.add('is-flipped'));
        console.log(`${id.padEnd(13)} front side shows ${before.padEnd(13)}  flipped shows ${await shows()}`);
    }
    // 7) SVG: rect center (in CSS px) before rotation is (50, 50)
    for (const id of ['wheel-view', 'wheel-fill']) {
        const c = await page.locator(`#${id}`).evaluate(el => {
            const b = el.getBoundingClientRect(), s = el.ownerSVGElement.getBoundingClientRect();
            return `(${Math.round(b.x - s.x + b.width / 2)}, ${Math.round(b.y - s.y + b.height / 2)})`;
        });
        const box = await page.locator(`#${id}`).evaluate(el => getComputedStyle(el).transformBox);
        console.log(`${id.padEnd(10)} transform-box ${box.padEnd(8)} center after rotate ${c}`);
    }
    await browser.close();
}
