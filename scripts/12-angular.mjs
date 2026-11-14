import { openAngular } from './angular-page.mjs';

// emulated encapsulation: which links did the component's "a" rule reach?
for (const engine of ['chromium', 'webkit']) {
    const { browser, page } = await openAngular(engine);
    console.log(`--- ${engine}\n${await page.evaluate(() => {
        const link = a => `${a.textContent.padEnd(14)} underline ${getComputedStyle(a).textDecorationLine === 'underline' ? 'yes' : 'no'}, ` +
            `attributes: ${[...a.attributes].map(x => x.name).join(' ')}`;
        const rule = [...document.styleSheets].flatMap(s => [...s.cssRules]).map(r => r.cssText).find(t => t.startsWith('a['));
        return [...document.querySelectorAll('app-book-card a')].map(link).join('\n') + `\n${rule}`;
    })}`);
    await browser.close();
}
