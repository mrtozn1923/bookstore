import { open } from './browser.mjs';

// freeze every running animation and transition at the same moment
const seek = (elements, ms) => elements.flatMap(e => e.getAnimations()).forEach(a => { a.pause(); a.currentTime = ms; });

const results = {};
for (const engine of ['chromium', 'webkit']) {
    const { browser, page } = await open('lab/13-motion.html', { engine });
    results[engine] = await page.evaluate(seekSource => {
        const seek = eval(seekSource);
        const lines = ['easing      200ms   500ms   800ms'];
        const tracks = [...document.querySelectorAll('.track')];
        document.getElementById('easings').classList.add('moved');
        const rows = new Map(tracks.map(t => [t.id, []]));
        for (const ms of [200, 500, 800]) {
            seek(tracks, ms);
            for (const t of tracks) rows.get(t.id).push(parseFloat(getComputedStyle(t).translate).toFixed(1).padStart(6) + 'px');
        }
        for (const [id, values] of rows) lines.push(`${id.padEnd(10)}${values.join('')}`);

        // a transition interrupted half way and sent back
        const chip = document.getElementById('chip');
        chip.classList.add('on');
        seek([chip], 500);
        const at = getComputedStyle(chip).opacity;
        chip.classList.remove('on');
        const back = chip.getAnimations()[0];
        lines.push(`chip: opacity ${at} at 500ms, then back to 0.2 in ${back.effect.getComputedTiming().duration}ms`);

        // keyframes during the 200ms delay
        const counts = [...document.querySelectorAll('.count')];
        seek(counts, 100);
        lines.push(`during the delay: fill-mode none scale ${getComputedStyle(counts[0]).scale}, backwards scale ${getComputedStyle(counts[1]).scale}`);
        return lines.join('\n');
    }, `(${seek})`);
    await browser.close();
}
console.log(`--- chromium\n${results.chromium}`);
console.log(`--- webkit: ${results.webkit === results.chromium ? 'the same values' : `\n${results.webkit}`}`);
