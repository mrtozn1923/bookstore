import { open, engines } from './browser.mjs';
import { countColor } from './pixels.mjs';

for (const engine of engines) {
    const { browser, page } = await open('book.html', { engine, width: 420, height: 900 });
    const summary = page.locator('#summary');
    const button = page.locator('.read-more');
    const state = async () => {
        const box = await summary.boundingBox();
        return `height ${box.height}px, button "${await button.textContent()}" `
            + `aria-expanded=${await button.getAttribute('aria-expanded')}`;
    };
    console.log(`--- ${engine}`);
    await button.waitFor();
    console.log(`clamped:  ${await state()}`);
    // is the red ::first-letter painted? (accent color #b91c1c)
    const box = await summary.boundingBox();
    const red = await countColor(page, { x: box.x, y: box.y, width: 48, height: 48 }, [185, 28, 28]);
    console.log(`red ::first-letter pixels while clamped: ${red}`);
    console.log(`text in the DOM: ${(await summary.textContent()).trim().split(/\s+/).length} words`);
    await button.click();
    console.log(`expanded: ${await state()}`);
    const red2 = await countColor(page, { x: box.x, y: box.y, width: 48, height: 48 }, [185, 28, 28]);
    console.log(`red ::first-letter pixels when expanded: ${red2}`);
    const review = page.locator('.review-text');
    console.log(`review: ${await review.evaluate(el => {
        const range = document.createRange();
        range.selectNodeContents(el);
        const tops = new Set([...range.getClientRects()].map(r => Math.round(r.top)));
        return `${tops.size} lines, scrollWidth ${el.scrollWidth} = clientWidth ${el.clientWidth}`;
    })}`);
    await browser.close();
}
