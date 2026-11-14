import { createHash } from 'node:crypto';
import { open, engines } from './browser.mjs';

const hash = buffer => createHash('sha1').update(buffer).digest('hex').slice(0, 8);

for (const engine of engines) {
    const { browser, page, requests } = await open('lab/01-fonts.html', { engine });
    console.log(`--- ${engine}`);
    console.log(await page.evaluate(() => {
        const css = (el, prop) => getComputedStyle(el).getPropertyValue(prop);
        const width = el => el.getBoundingClientRect().width.toFixed(1);
        const lines = [];

        // 1) what the font shorthand resets
        const title = document.getElementById('title');
        lines.push(`.card line-height: ${css(title.parentElement, 'line-height')}`
            + ` | .title line-height: ${css(title, 'line-height')}`
            + ` | font-variation-settings: ${css(title, 'font-variation-settings')}`);

        // 2) one variable font, many weights
        for (const el of document.querySelectorAll('.weights span, .single span')) {
            lines.push(`${el.closest('p').className.padEnd(8)} ${el.className.padEnd(18)}`
                + ` font-weight: ${css(el, 'font-weight')}  width: ${width(el)}px`);
        }
        // 3) synthesized italic keeps the upright advance widths
        for (const el of document.querySelectorAll('.upright span, .generic span, .optical span')) {
            lines.push(`${el.closest('p').className.padEnd(8)} ${el.className.padEnd(18)} width: ${width(el)}px`);
        }
        return lines.join('\n');
    }));

    // pixel comparison: is the rendering identical?
    const shot = async selector => hash(await page.locator(selector).first()
        .screenshot({ animations: 'disabled', omitBackground: true }));
    for (const [a, b] of [['.single .w400', '.single .w700'], ['.single .w400', '.single .no-synth'],
                          ['.upright .plain', '.upright .italic'], ['.upright .plain', '.upright .no-synth']]) {
        console.log(`same pixels? ${a} vs ${b}: ${await shot(a) === await shot(b)}`);
    }

    // which font files did the page request?
    const fonts = new Set(requests.filter(r => r.endsWith('.woff2')).map(r => r.split('/').pop()));
    console.log('font requests:', [...fonts].join(', '));

    // Chromium only: which font actually drew the glyphs (the same data DevTools shows under "Rendered Fonts")
    if (engine === 'chromium') {
        const cdp = await page.context().newCDPSession(page);
        await cdp.send('DOM.enable');
        await cdp.send('CSS.enable');
        const { root } = await cdp.send('DOM.getDocument');
        for (const selector of ['.glyphs .full', '.glyphs .latin-only']) {
            const { nodeId } = await cdp.send('DOM.querySelector', { nodeId: root.nodeId, selector });
            const { fonts } = await cdp.send('CSS.getPlatformFontsForNode', { nodeId });
            console.log(`${selector.padEnd(20)} ${fonts.map(f => `${f.familyName} x${f.glyphCount}`).join(', ')}`);
        }
    }
    await browser.close();
}
