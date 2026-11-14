import { open, engines } from './browser.mjs';

for (const engine of engines) {
    const { browser, page } = await open('lab/02-text.html', { engine });
    console.log(`--- ${engine}`);
    const measure = () => page.evaluate(() => {
        const css = (el, prop) => getComputedStyle(el).getPropertyValue(prop);
        const $ = selector => document.querySelector(selector);
        const lines = [];

        // 1) letter-spacing: em vs % on a 12px parent and a 24px child
        for (const selector of ['.badge', '.badge-pct']) {
            lines.push(`${selector.padEnd(17)} font-size ${css($(selector), 'font-size')}`
                + `  letter-spacing ${css($(selector), 'letter-spacing')}`);
        }
        for (const selector of ['.badge .big', '.badge-pct .big', '.badge-none .big']) {
            lines.push(`${selector.padEnd(17)} font-size ${css($(selector), 'font-size')}`
                + `  letter-spacing ${css($(selector), 'letter-spacing').padEnd(6)}`
                + `  width ${$(selector).getBoundingClientRect().width.toFixed(1)}px`);
        }
        // 2) what does the text-decoration shorthand reset?
        for (const selector of ['.link', '.link.reset']) {
            lines.push(`${selector.padEnd(16)} thickness ${css($(selector), 'text-decoration-thickness')}`
                + `  offset ${css($(selector), 'text-underline-offset')}`);
        }
        // 4) the gap below an inline svg
        for (const el of document.querySelectorAll('.icon-box')) {
            lines.push(`${el.className.padEnd(16)} box height ${el.getBoundingClientRect().height}px (svg 24px)`);
        }
        // 5) icon bottom relative to the text baseline (the zero-size probe sits on the baseline)
        for (const row of document.querySelectorAll('.price > span')) {
            const baseline = row.querySelector('.probe').getBoundingClientRect().bottom;
            const icon = row.querySelector('svg').getBoundingClientRect();
            lines.push(`${row.className.padEnd(16)} icon top ${(icon.top - baseline).toFixed(1)}px`
                + `  bottom ${(icon.bottom - baseline).toFixed(1)}px from baseline`);
        }
        // 6) text-transform follows the lang attribute
        for (const el of document.querySelectorAll('p [lang]')) {
            lines.push(`lang=${el.lang} ${css(el, 'text-transform').padEnd(10)} → ${el.innerText}`);
        }
        // 7) text-indent: first line vs the others
        for (const el of document.querySelectorAll('.indent')) {
            const range = document.createRange();
            range.selectNodeContents(el);
            const [first, second] = range.getClientRects();
            const left = el.getBoundingClientRect().left;
            lines.push(`${el.className.padEnd(16)} line 1 starts at ${(first.left - left).toFixed(1)}px,`
                + ` line 2 at ${(second.left - left).toFixed(1)}px`);
        }
        lines.push(`text-shadow: ${css($('.shadow'), 'text-shadow')}`);
        return lines.join('\n');
    });
    console.log(await measure());

    // 3) hover changes the underline, not the box
    const link = page.locator('.hover-test');
    const before = await link.boundingBox();
    await link.hover();
    const after = await link.boundingBox();
    const thickness = await link.evaluate(el => getComputedStyle(el).textDecorationThickness);
    console.log(`hover: thickness ${thickness}, box ${before.width}x${before.height} → ${after.width}x${after.height}`);
    await browser.close();
}
