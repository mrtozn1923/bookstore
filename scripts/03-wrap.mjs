import { open, engines } from './browser.mjs';

for (const engine of engines) {
    const { browser, page } = await open('lab/03-wrap.html', { engine });
    console.log(`--- ${engine}`);
    console.log(await page.evaluate(() => {
        const css = (el, prop) => getComputedStyle(el).getPropertyValue(prop);
        // rendered lines and the width of each line, from the text's client rects
        const lineWidths = el => {
            const range = document.createRange();
            range.selectNodeContents(el);
            const rows = new Map();
            for (const r of range.getClientRects()) {
                const key = Math.round(r.top);
                rows.set(key, (rows.get(key) ?? 0) + r.width);
            }
            return [...rows.values()].map(w => Math.round(w));
        };
        const name = el => el.className.padEnd(20);
        const lines = [];

        for (const el of document.querySelectorAll('.fit')) {
            lines.push(`${name(el)} box width ${el.getBoundingClientRect().width.toFixed(1)}px`);
        }
        for (const el of document.querySelectorAll('.narrow, .review')) {
            lines.push(`${name(el)} ${lineWidths(el).length} lines  (${css(el, 'white-space-collapse')} / ${css(el, 'text-wrap-mode')})`);
        }
        for (const el of document.querySelectorAll('.hyphen')) {
            lines.push(`${name(el)} lang=${el.lang} ${lineWidths(el).length} lines: ${lineWidths(el).join(' ')}`);
        }
        for (const el of document.querySelectorAll('.title, .para')) {
            lines.push(`${name(el)} line widths: ${lineWidths(el).join(' ')}`);
        }
        for (const el of document.querySelectorAll('.summary')) {
            lines.push(`${name(el)} height ${el.getBoundingClientRect().height}px (line-height ${css(el, 'line-height')})`);
        }
        for (const el of document.querySelectorAll('.btn')) {
            lines.push(`${name(el)} height ${el.getBoundingClientRect().height.toFixed(1)}px`);
        }
        lines.push(`supports: line-clamp ${CSS.supports('line-clamp: 3')}, -webkit-line-clamp ${CSS.supports('-webkit-line-clamp: 3')},`
            + ` text-wrap: pretty ${CSS.supports('text-wrap: pretty')}, text-box ${CSS.supports('text-box: trim-both cap alphabetic')}`);
        return lines.join('\n');
    }));
    await browser.close();
}
