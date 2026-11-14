import { open, engines } from './browser.mjs';
import { pixels } from './pixels.mjs';

const rgb = d => `rgb(${d[0]}, ${d[1]}, ${d[2]})`;
async function colorAt(page, locator, x, y) {
    const box = await locator.boundingBox();
    return rgb((await pixels(page, { x: box.x + x, y: box.y + y, width: 1, height: 1 })).data);
}

for (const engine of engines) {
    const { browser, page } = await open('lab/08-clip-mask.html', { engine });
    console.log(`--- ${engine}`);
    // 1) the corner (5, 5): painted? hit by a click? the box keeps its size
    for (const id of ['inset', 'circle', 'polygon']) {
        const el = page.locator(`#${id}`);
        const b = await el.boundingBox();
        const hit = await page.evaluate(([x, y]) => document.elementFromPoint(x, y).id || document.elementFromPoint(x, y).tagName,
            [b.x + 5, b.y + 5]);
        const center = await page.evaluate(([x, y]) => document.elementFromPoint(x, y).id, [b.x + 100, b.y + 50]);
        console.log(`${id.padEnd(8)} box ${b.width}x${b.height}  corner pixel ${await colorAt(page, el, 5, 5)}  `
            + `click at corner hits ${hit}, at center hits ${center}`);
    }
    console.log(`shadowed 3px outside the box: ${await colorAt(page, page.locator('#shadowed'), 100, -3)}`);
    // 2) the same clip on a 200px and a 400px box: a point at 75% width, and the bottom middle
    for (const sel of ['.wave.path:not(.wide)', '.wave.path.wide', '.wave.shape:not(.wide)', '.wave.shape.wide']) {
        const el = page.locator(sel);
        const b = await el.boundingBox();
        console.log(`${sel.replace(':not(.wide)', '').padEnd(16)} width ${String(b.width).padEnd(3)}`
            + ` (${b.width * 0.75}, 40): ${await colorAt(page, el, b.width * 0.75, 40)}`
            + `  (${b.width / 2}, 80): ${await colorAt(page, el, b.width / 2, 80)}`);
    }
    // 3) the strip: left edge, 20px in, the middle
    const strip = page.locator('.strip');
    console.log(`strip mask  x=1: ${await colorAt(page, strip, 1, 20)}  x=20: ${await colorAt(page, strip, 20, 20)}`
        + `  x=150: ${await colorAt(page, strip, 150, 20)}`);
    // 4) where do the first lines of the paragraph start (x from the left of the section)?
    for (const id of ['boxed', 'shaped']) {
        const starts = await page.locator(`#${id}`).evaluate(section => {
            const range = document.createRange();
            range.selectNodeContents(section.querySelector('p'));
            const left = section.getBoundingClientRect().left;
            const lines = new Map();
            for (const r of range.getClientRects()) {
                const y = Math.round(r.top);
                if (!lines.has(y) || r.left < lines.get(y)) lines.set(y, r.left);
            }
            return [...lines.values()].slice(0, 8).map(x => Math.round(x - left));
        });
        console.log(`${id.padEnd(6)} line starts: ${starts.join(', ')}`);
    }
    await browser.close();
}

// the mask icon: normal and forced colors (background-color becomes the page color)
for (const forcedColors of ['none', 'active']) {
    const { browser, page } = await open('lab/08-icon.html', { engine: 'chromium', forcedColors });
    console.log(`--- chromium, forced colors ${forcedColors}`);
    for (const id of ['icon-red', 'icon-fixed']) {
        console.log(`${id.padEnd(10)} background-color ${await page.locator(`#${id}`).evaluate(el => getComputedStyle(el).backgroundColor)}`
            + `, center pixel ${await colorAt(page, page.locator(`#${id}`), 24, 24)}`);
    }
    await browser.close();
}
