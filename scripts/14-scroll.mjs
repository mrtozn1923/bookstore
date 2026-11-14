import { open } from './browser.mjs';

// scroll-driven animations update on the next frame after a scroll
const frame = () => new Promise(r => requestAnimationFrame(() => requestAnimationFrame(r)));

const results = {};
for (const engine of ['chromium', 'webkit']) {
    const lines = [];

    // 1) the timelines in a 200px scroller
    let { browser, page } = await open('lab/14-timeline.html', { engine });
    lines.push(...await page.evaluate(async frameSource => {
        const frame = eval(frameSource);
        const scroller = document.getElementById('scroller');
        const value = id => Number(getComputedStyle(document.getElementById(id)).getPropertyValue('--progress')).toFixed(2);
        const rows = ['scrollTop  scroll()  cover  entry  contain  exit'];
        for (const top of [0, 100, 150, 200, 250, 300, 350, 400, 500]) {
            scroller.scrollTop = top;
            await frame();
            rows.push(`${String(top).padStart(5)}px ${value('scroll').padStart(8)} ${['cover', 'entry', 'contain', 'exit'].map(id => value(id).padStart(6)).join(' ')}`);
        }
        rows.push(`timeline before the shorthand: ${getComputedStyle(document.getElementById('wrong')).animationTimeline}, --progress ${value('wrong')}`);
        return rows;
    }, `(${frame})`));
    await browser.close();

    // 2) Bookstore: the reading guide's progress bar on a phone-sized screen
    ({ browser, page } = await open('guide.html', { engine, width: 360, height: 640 }));
    lines.push(await page.evaluate(async frameSource => {
        const frame = eval(frameSource);
        const max = document.documentElement.scrollHeight - innerHeight;
        const bar = document.querySelector('.reading-progress');
        const out = [];
        for (const part of [0, 0.25, 0.5, 1]) {
            scrollTo({ top: Math.round(max * part), behavior: "instant" });
            await frame();
            out.push(`${part * 100}% ${getComputedStyle(bar).scale}`);
        }
        return `guide.html, ${max}px to scroll: ${out.join(' | ')}`;
    }, `(${frame})`));
    await browser.close();

    // 3) Bookstore: the catalog's cards as they come into view
    for (const reducedMotion of ['no-preference', 'reduce']) {
        ({ browser, page } = await open('catalog.html', { engine, width: 360, height: 640, reducedMotion }));
        lines.push(await page.evaluate(async frameSource => {
            const frame = eval(frameSource);
            const card = [...document.querySelectorAll('.book-list > .book-card')].at(-1);
            const out = [];
            // from just below the screen until the whole card has come in
            const top = card.getBoundingClientRect().top + scrollY;
            for (const offset of [0, 0.5, 1]) {
                scrollTo({ top: Math.round(top - innerHeight + card.offsetHeight * offset), behavior: "instant" });
                await frame();
                const cs = getComputedStyle(card);
                const y = cs.translate === 'none' ? 'none' : `${parseFloat(cs.translate.split(' ')[1]).toFixed(1)}px`;
                out.push(`${offset * 100}% in: opacity ${Number(cs.opacity).toFixed(2)}, translate y ${y}`);
            }
            return `"${card.querySelector('.book-title').textContent}" ${out.join(' | ')}`;
        }, `(${frame})`) + ` (${reducedMotion})`);
        await browser.close();
    }
    results[engine] = lines.join('\n');
}
console.log(`--- chromium\n${results.chromium}`);
console.log(`--- webkit: ${results.webkit === results.chromium ? 'the same values' : `\n${results.webkit}`}`);
