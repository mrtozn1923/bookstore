import { open } from './browser.mjs';

for (const engine of ['chromium', 'webkit']) {
    const { browser, page } = await open('lab/04-flow.html', { engine });
    console.log(`--- ${engine}`);
    console.log(await page.evaluate(() => {
        const lines = [];
        for (const id of ['plain', 'contained']) {
            const review = document.getElementById(id);
            lines.push(`${id.padEnd(10)} article height: ${review.getBoundingClientRect().height}px, ` +
                `floated cover: ${review.querySelector('img').getBoundingClientRect().height}px`);
        }
        for (const probe of document.querySelectorAll('#probes span')) {
            lines.push(`${probe.getAttribute('style').padEnd(30)} computed: ${getComputedStyle(probe).display}`);
        }
        return lines.join('\n');
    }));
    await browser.close();
}
