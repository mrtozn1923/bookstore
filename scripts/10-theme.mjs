import { open } from './browser.mjs';

const colours = () => {
    const root = getComputedStyle(document.documentElement);
    const card = getComputedStyle(document.querySelector('.book-list .book-card:not(:has(> img))'));
    return `data-theme=${document.documentElement.dataset.theme ?? '(none)'} color-scheme ${root.colorScheme}: ` +
        `page ${root.backgroundColor} text ${root.color} | card without cover ${card.backgroundColor} | ` +
        `toggle aria-pressed=${document.querySelector('.theme-toggle').getAttribute('aria-pressed')}`;
};

for (const engine of ['chromium', 'webkit']) {
    console.log(`--- ${engine}`);

    // 1) custom property behaviour in the lab
    let { browser, page } = await open('lab/10-custom-properties.html', { engine });
    console.log(await page.evaluate(async () => {
        const cs = id => getComputedStyle(document.getElementById(id));
        const stock = document.getElementById('stock');
        stock.classList.add('full');
        // freeze both transitions half way
        for (const a of stock.getAnimations()) { a.pause(); a.currentTime = 500; }
        return [
            `badge in card ${cs('in-card').color}, outside ${cs('outside').color}`,
            `price with --accent: 20px -> color ${cs('price').color}`,
            `half way: --stock-level ${cs('stock').getPropertyValue('--stock-level')}, --plain-level ${cs('stock').getPropertyValue('--plain-level')}`,
            `child of .stock sees --stock-level ${cs('stock-child').getPropertyValue('--stock-level')}`,
            `--stock-level: 50% then 20px -> ${cs('bad-stock').getPropertyValue('--stock-level')}`,
            `light-dark() token: ${cs('light-surface').backgroundColor} outside, ${cs('dark-surface').backgroundColor} in the dark band`,
        ].join('\n');
    }));
    await browser.close();

    // 2) the system preference, then the user's own choice, then a reload
    for (const colorScheme of ['light', 'dark']) {
        ({ browser, page } = await open('catalog.html', { engine, colorScheme }));
        console.log(`system ${colorScheme.padEnd(5)}          ${await page.evaluate(colours)}`);
        await browser.close();
    }
    ({ browser, page } = await open('catalog.html', { engine, colorScheme: 'dark' }));
    await page.click('.theme-toggle');
    console.log(`system dark, clicked     ${await page.evaluate(colours)}`);
    await page.reload();
    console.log(`after reload             ${await page.evaluate(colours)}`);
    console.log(`category dots: ${await page.evaluate(() => [...document.querySelectorAll('.cat-link')]
        .map(a => `${a.textContent} ${getComputedStyle(a, '::before').backgroundColor}`).join(', '))}`);
    await browser.close();
}
