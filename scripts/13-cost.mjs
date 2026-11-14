import { open } from './browser.mjs';

// Chromium only: CDP counts layouts, style recalculations and compositing layers
const { browser, page } = await open('lab/13-compositor.html', { width: 800, height: 600 });
const cdp = await page.context().newCDPSession(page);
await cdp.send('Performance.enable');
const metrics = async () => Object.fromEntries((await cdp.send('Performance.getMetrics')).metrics.map(m => [m.name, m.value]));

for (const mode of ['margin', 'transform']) {
    await page.evaluate(m => document.body.className = m, mode);
    const before = await metrics();
    await page.waitForTimeout(1000);
    const after = await metrics();
    const delta = name => after[name] - before[name];
    console.log(`${mode.padEnd(9)} 1s: layouts ${String(delta('LayoutCount')).padStart(3)}, style recalcs ${String(delta('RecalcStyleCount')).padStart(3)}`);
}

// how many compositing layers the page has, with and without will-change on 50 cards
await cdp.send('LayerTree.enable');
for (const mode of ['', 'promoted']) {
    const layers = new Promise(resolve => cdp.once('LayerTree.layerTreeDidChange', e => resolve(e.layers?.length)));
    await page.evaluate(m => document.body.className = m, mode);
    console.log(`will-change ${mode ? 'on 50 cards' : 'off        '}: ${await layers} layers`);
}
await browser.close();
