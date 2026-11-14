import { open } from './browser.mjs';
import { explain, print } from './cascade-report.mjs';

const report = () => {
    const cs = id => getComputedStyle(document.getElementById(id));
    const link = id => `${id.padEnd(14)} ${cs(id).color.padEnd(20)} underline ${cs(id).textDecorationLine === 'underline' ? 'yes' : 'no'}`;
    const shadowP = getComputedStyle(document.getElementById('note').shadowRoot.querySelector('p'));
    return [
        ...['leaky-title', 'leaky-note', 'scoped-title', 'scoped-note', 'light', 'dark-in-light', 'light-in-dark'].map(link),
        `scoped price   ${cs('scoped-price').color}`,
        `shadow p: margin-top ${shadowP.marginTop}, weight ${shadowP.fontWeight}, color ${shadowP.color} | ` +
            `page p: weight ${cs('light-dom-p').fontWeight}`,
    ].join('\n');
};

for (const engine of ['chromium', 'webkit']) {
    const { browser, page } = await open('lab/12-scope.html', { engine });
    console.log(`--- ${engine}\n${await page.evaluate(report)}`);
    if (engine === 'chromium') print(await explain(page, '#scoped-price', 'color'), 'color');
    await browser.close();
}

// the same donut scope on the Bookstore catalog page
for (const engine of ['chromium', 'webkit']) {
    const { browser, page } = await open('catalog.html', { engine });
    console.log(`${engine} catalog: ${await page.evaluate(() => [...document.querySelectorAll('.featured a')].map(a => {
        const cs = getComputedStyle(a);
        return `"${a.textContent}" ${cs.color} underline ${cs.textDecorationLine === 'underline' ? 'yes' : 'no'}`;
    }).join(', '))}`);
    await browser.close();
}
