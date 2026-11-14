import { open } from './browser.mjs';

// helpers that run in the page: any CSS colour -> sRGB hex via canvas, WCAG 2 contrast ratio
const helpers = () => {
    const ctx = document.createElement('canvas').getContext('2d', { willReadFrequently: true });
    window.toRgb = css => {
        ctx.clearRect(0, 0, 1, 1);
        ctx.fillStyle = css;
        ctx.fillRect(0, 0, 1, 1);
        const [r, g, b, a] = ctx.getImageData(0, 0, 1, 1).data;
        return { r, g, b, a, hex: '#' + [r, g, b].map(v => v.toString(16).padStart(2, '0')).join('') };
    };
    const lin = v => (v /= 255) <= 0.04045 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4;
    window.luminance = ({ r, g, b }) => 0.2126 * lin(r) + 0.7152 * lin(g) + 0.0722 * lin(b);
    window.ratio = (x, y) => {
        const [hi, lo] = [luminance(x), luminance(y)].sort((p, q) => q - p);
        return ((hi + 0.05) / (lo + 0.05)).toFixed(2);
    };
};

for (const engine of ['chromium', 'webkit']) {
    console.log(`--- ${engine}`);
    const { browser, page } = await open('lab/11-color.html', { engine });
    await page.evaluate(helpers);
    console.log(await page.evaluate(() => {
        const bg = id => getComputedStyle(document.getElementById(id)).backgroundColor;
        const white = { r: 255, g: 255, b: 255 };
        const lines = [];
        for (const id of ['hsl-yellow', 'hsl-blue', 'oklch-yellow', 'oklch-blue']) {
            const c = toRgb(bg(id));
            lines.push(`${id.padEnd(13)} ${c.hex}  luminance ${luminance(c).toFixed(3)}  contrast with white ${ratio(c, white)}`);
        }
        for (const id of ['base', 'oklch-of-hex', 'darker', 'soft', 'mix-black', 'mix-srgb', 'mix-oklch']) {
            lines.push(`${id.padEnd(13)} ${bg(id).padEnd(40)} = ${toRgb(bg(id)).hex}`);
        }
        lines.push(`faded         ${bg('faded')}`);
        for (const id of ['accent-light', 'accent-dark', 'royal-blue']) {
            const cs = getComputedStyle(document.getElementById(id));
            const back = toRgb(cs.backgroundColor);
            lines.push(`${id.padEnd(13)} on ${back.hex}: contrast-color() = ${cs.color}; ` +
                `black ${ratio(back, { r: 0, g: 0, b: 0 })}, white ${ratio(back, white)}`);
        }
        const w = id => getComputedStyle(document.getElementById(id)).inlineSize;
        lines.push(`round(up, 0.9rem, 4px) = ${w('round')}, mod(-7px, 3px) = ${w('mod')}, ` +
            `rem(-7px, 3px) + 10px = ${w('rem')}, abs(-12px) * sign(-0.5) + 20px = ${w('abs')}`);
        const fit = getComputedStyle(document.getElementById('fit'));
        lines.push(`480px shelf: --cols ${fit.getPropertyValue('--cols')}, progress(100cqi, 20rem, 40rem) ${fit.opacity}`);
        lines.push(`if() ${w('if-test')}, @function ${w('function-test')}, random() step 50px ${['100px', '150px', '200px'].includes(w('random-test')) ? 'one of 100/150/200px' : w('random-test')}`);
        return lines.join('\n');
    }));
    await browser.close();
}

// the same functions in Bookstore, in the light and the dark theme
for (const engine of ['chromium', 'webkit']) {
    for (const colorScheme of ['light', 'dark']) {
        let { browser, page } = await open('book.html', { engine, colorScheme });
        await page.hover('.add-to-cart');
        const button = await page.evaluate(() => {
            const cs = getComputedStyle(document.querySelector('.add-to-cart'));
            return `${cs.color} on ${cs.backgroundColor}`;
        });
        await browser.close();
        ({ browser, page } = await open('catalog.html', { engine, colorScheme }));
        await page.hover('.cat-link');
        const result = await page.evaluate(() => {
            const badge = getComputedStyle(document.querySelector('.badge'));
            return `badge ${badge.color} on ${badge.backgroundColor} | category hover ` +
                getComputedStyle(document.querySelector('.cat-link')).backgroundColor;
        });
        console.log(`${engine} ${colorScheme.padEnd(5)} add-to-cart hover ${button} | ${result}`);
        await browser.close();
    }
}
