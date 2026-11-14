import { open, engines } from './browser.mjs';
import { pixels } from './pixels.mjs';

// WCAG contrast of white text against the brightest background pixel behind each text element
async function worstContrast(page) {
    await page.addStyleTag({ content: '.promo-banner * { color: transparent !important; }' });
    const lum = c => {
        const v = c / 255;
        return v <= 0.04045 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4;
    };
    const result = [];
    for (const selector of ['h2', 'p']) {
        const { data } = await pixels(page, await page.locator(`.promo-banner ${selector}`).boundingBox());
        let max = 0;
        for (let i = 0; i < data.length; i += 4) {
            max = Math.max(max, 0.2126 * lum(data[i]) + 0.7152 * lum(data[i + 1]) + 0.0722 * lum(data[i + 2]));
        }
        result.push(`${selector} ${(1.05 / (max + 0.05)).toFixed(2)}:1`);
    }
    return result.join(', ');
}

// splits a computed background-image into its top-level layers
const layers = value => {
    const out = [];
    let depth = 0, start = 0;
    for (let i = 0; i < value.length; i++) {
        if (value[i] === '(') depth++;
        if (value[i] === ')') depth--;
        if (value[i] === ',' && depth === 0) { out.push(value.slice(start, i)); start = i + 1; }
    }
    return [...out, value.slice(start)].map(layer => layer.trim().split('(')[0]);
};

for (const engine of engines) {
    console.log(`--- ${engine}`);
    for (const deviceScaleFactor of [1, 2]) {
        const { browser, page, requests } = await open('index.html', { engine, deviceScaleFactor });
        const images = requests.filter(r => r.startsWith('/img/')).join(', ');
        console.log(`dpr ${deviceScaleFactor}: requested ${images}`);
        if (deviceScaleFactor === 1) {
            const value = await page.locator('.promo-banner').evaluate(el => getComputedStyle(el).backgroundImage);
            console.log(`banner layers: ${layers(value).join(' + ')}`);
            console.log(`worst contrast with overlay:    ${await worstContrast(page)}`);
            await page.locator('.promo-banner').evaluate(el => {
                el.style.backgroundImage = 'image-set(url("img/banner-1x.avif") 1x type("image/avif"))';
            });
            console.log(`worst contrast without overlay: ${await worstContrast(page)}`);
        }
        await browser.close();
    }
    for (const reducedMotion of ['no-preference', 'reduce']) {
        const { browser, page } = await open('index.html', { engine, reducedMotion });
        const animation = await page.locator('.skeleton').first()
            .evaluate(el => getComputedStyle(el).animationName);
        console.log(`skeleton animation (${reducedMotion}): ${animation}`);
        await browser.close();
    }
}
