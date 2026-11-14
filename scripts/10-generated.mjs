import { open, engines } from './browser.mjs';
import { pixels, countColor } from './pixels.mjs';
import { axNames, axItems } from './ax.mjs';

const rgb = d => `rgb(${d[0]}, ${d[1]}, ${d[2]})`;

for (const engine of engines) {
    const { browser, page } = await open('lab/10-generated.html', { engine });
    console.log(`--- ${engine}`);
    // 1) the ::before box exists only with content
    for (const id of ['no-content', 'with-content']) {
        const out = await page.locator(`#${id}`).evaluate(el =>
            `content ${getComputedStyle(el, '::before').content}, span width ${Math.round(el.getBoundingClientRect().width)}`);
        console.log(`${id.padEnd(12)} ::before ${out}`);
    }
    // 2) the top left pixel of each cover
    for (const id of ['cover-ok', 'cover-broken']) {
        const b = await page.locator(`#${id}`).boundingBox();
        console.log(`${id.padEnd(12)} pixel (5, 5): ${rgb((await pixels(page, { x: b.x + 5, y: b.y + 5, width: 1, height: 1 })).data)}`);
    }
    // 6) where does the text start? the marker sits inside the line
    for (const id of ['default-marker', 'string-marker', 'marker-content']) {
        const x = await page.locator(`#${id} li`).evaluate(li => {
            const range = document.createRange();
            range.selectNodeContents(li);
            return Math.round(range.getClientRects()[0].left - li.getBoundingClientRect().left);
        });
        const b = await page.locator(`#${id} li`).boundingBox();
        const red = await countColor(page, { x: b.x, y: b.y, width: 18, height: b.height }, [185, 28, 28], 60);
        console.log(`${id.padEnd(14)} text starts at x=${x}, red marker pixels: ${red}`);
    }
    if (engine === 'chromium') {
        // 3) accessible names, 4) and 5) generated numbers as the accessibility tree sees them
        const [plain, alt, ext] = await axNames(page, '#badge-plain, #badge-alt, #ext');
        console.log(`accessible names: badge-plain "${plain}", badge-alt "${alt}", ext "${ext}"`);
        console.log(`ext innerText: "${await page.locator('#ext').innerText()}"`);
        console.log(`steps:   ${await axItems(page, '#steps', '')}`);
        console.log(`answers: ${await axItems(page, '#answers', '')}`);
    }
    await browser.close();
}
