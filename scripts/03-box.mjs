import { open } from './browser.mjs';

for (const engine of ['chromium', 'webkit']) {
    const { browser, page } = await open('lab/03-box.html', { engine });
    console.log(`--- ${engine}`);
    console.log(await page.evaluate(() => {
        const rect = el => el.getBoundingClientRect();
        const lines = [];

        // 1) box-sizing: the same width, two different boxes
        for (const el of document.querySelectorAll('.box')) {
            lines.push(`${el.textContent.padEnd(12)} rendered width: ${rect(el).width}px`);
        }

        // 2) margin collapse: the gap between two paragraphs
        for (const id of ['siblings', 'negative', 'flex']) {
            const [a, b] = document.querySelectorAll(`#${id} > p`);
            lines.push(`gap (${id}):`.padEnd(18) + `${rect(b).top - rect(a).bottom}px`);
        }

        // 3) does the child's margin escape the parent?
        for (const id of ['escape', 'contained']) {
            const section = document.getElementById(id);
            const h2 = section.querySelector('h2');
            lines.push(`${id.padEnd(10)} space between section top and h2: ${rect(h2).top - rect(section).top}px`);
        }

        // 4) intrinsic sizes inside a 300px shelf
        for (const el of document.querySelectorAll('.shelf > p')) {
            lines.push(`${el.className.padEnd(4)} width: ${String(rect(el).width.toFixed(1)).padEnd(6)} ` +
                `lines: ${Math.round(rect(el).height / parseFloat(getComputedStyle(el).lineHeight))}`);
        }
        return lines.join('\n');
    }));

    // 5) viewport units on a phone-sized viewport
    await page.setViewportSize({ width: 390, height: 844 });
    console.log(await page.evaluate(() => ['vh', 'svh', 'lvh', 'dvh'].map(unit => {
        const probe = document.createElement('div');
        probe.style.height = `100${unit}`;
        document.body.append(probe);
        const height = probe.getBoundingClientRect().height;
        probe.remove();
        return `100${unit}=${height}px`;
    }).join('  ')));
    await browser.close();
}
