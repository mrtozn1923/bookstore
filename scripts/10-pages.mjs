import { open, engines } from './browser.mjs';
import { pixels, countColor } from './pixels.mjs';
import { axItems, axText } from './ax.mjs';

const rgb = d => `rgb(${d[0]}, ${d[1]}, ${d[2]})`;

for (const engine of engines) {
    console.log(`--- ${engine}`);
    const checkout = await open('checkout.html', { engine });
    const steps = checkout.page.locator('.checkout-steps li');
    console.log(`steps ::before: ${(await steps.evaluateAll(lis => lis.map(li => getComputedStyle(li, '::before').content))).join(' | ')}`);
    const circle = await steps.nth(1).evaluate(li => {
        const cs = getComputedStyle(li, '::before');
        return `${cs.width} x ${cs.height}, border-radius ${cs.borderTopLeftRadius}, color ${getComputedStyle(li).color}`;
    });
    console.log(`current step circle: ${circle}`);
    if (engine === 'chromium') console.log(`steps for screen readers: ${await axItems(checkout.page, '.checkout-steps')}`);
    await checkout.browser.close();

    const { browser, page } = await open('catalog.html', { engine });
    const badge = page.locator('.badge').first();
    const b = await badge.boundingBox();
    console.log(`badge ::after content ${await badge.evaluate(el => getComputedStyle(el, '::after').content)}, `
        + `pixel (width - 2, 4) ${rgb((await pixels(page, { x: b.x + b.width - 2, y: b.y + 4, width: 1, height: 1 })).data)}, `
        + `bottom left pixel ${rgb((await pixels(page, { x: b.x + 2, y: b.y + b.height - 2, width: 1, height: 1 })).data)}`);
    if (engine === 'chromium') console.log(`badge text for screen readers: "${await axText(page, '.badge')}"`);

    const items = page.locator('.reading-list li');
    console.log(`reading list markers: ${(await items.evaluateAll(lis => lis.map(li => getComputedStyle(li).listStyleType))).join(' | ')}`);
    const first = await items.first().boundingBox();
    console.log(`red marker pixels left of "Emma": ${await countColor(page, { x: first.x - 24, y: first.y, width: 24, height: first.height }, [185, 28, 28], 60)}`);
    if (engine === 'chromium') console.log(`reading list for screen readers: ${await axItems(page, '.reading-list ul')}`);
    console.log(`::selection background: ${await page.locator('h1').evaluate(el => getComputedStyle(el, '::selection').backgroundColor)}`);
    await browser.close();
}
