import { open } from './browser.mjs';
import { explain, print } from './cascade-report.mjs';

// which rules declared the property, with the specificity Chromium computed for the nested selectors
const { browser, page } = await open('lab/12-nesting.html');
for (const [selector, property] of [['.book-card__title', 'color'], ['.book-card .price', 'font-weight'], ['.highlight', 'color']]) {
    console.log(`--- ${selector}`);
    print(await explain(page, selector, property), property);
}
await browser.close();

// WebKit has no CDP; the computed values are enough to see that it agrees
const wk = await open('lab/12-nesting.html', { engine: 'webkit' });
console.log(`--- webkit: ${await wk.page.evaluate(() => {
    const cs = (s, p) => getComputedStyle(document.querySelector(s)).getPropertyValue(p);
    return `.book-card__title ${cs('.book-card__title', 'color')}, .price ${cs('.book-card .price', 'font-weight')}, ` +
        `.highlight ${cs('.highlight', 'color')}`;
})}`);
await wk.browser.close();
