import { open } from './browser.mjs';
import { explain, print } from './cascade-report.mjs';

async function report(file, checks) {
    const { browser, page } = await open(file);
    console.log(`--- ${file}`);
    for (const [selector, property] of checks) {
        print(await explain(page, selector, property), property);
    }
    await browser.close();
}

// 1) The bug: vendor CSS linked unlayered, our rule inside a layer
await report('lab/01-unlayered-vendor.html', [['.add-to-cart', 'background-color']]);
// 2) The fix: vendor imported into its own, lower layer
await report('book.html', [['.add-to-cart', 'background-color']]);
// 3) !important flips the layer order; the style attribute beats every layer
await report('lab/01-importance.html', [['.stock-note', 'display'], ['.add-to-cart', 'background-color']]);
