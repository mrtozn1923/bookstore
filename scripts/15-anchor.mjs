import { open } from './browser.mjs';

const frame = () => new Promise(r => requestAnimationFrame(() => requestAnimationFrame(r)));
const box = el => { const r = el.getBoundingClientRect(); return `${Math.round(r.x)},${Math.round(r.y)} ${Math.round(r.width)}x${Math.round(r.height)}`; };

const results = {};
for (const engine of ['chromium', 'webkit']) {
    const lines = [];

    // 1) anchor(), anchor-size(), position-area and flip-block in an 800x600 window
    let { browser, page } = await open('lab/15-anchor.html', { engine, width: 800, height: 600 });
    lines.push(await page.evaluate(boxSource => {
        const box = eval(boxSource);
        return ['a1', 't1', 'a2', 't2', 'a3', 't3', 't3-none']
            .map(id => `${id.padEnd(8)}${box(document.getElementById(id))}`).join('\n');
    }, `(${box})`));
    await browser.close();

    // 2) the pitfalls
    ({ browser, page } = await open('lab/15-pitfalls.html', { engine, width: 800, height: 600 }));
    lines.push(await page.evaluate(async ([boxSource, frameSource]) => {
        const [box, frame] = [eval(boxSource), eval(frameSource)];
        const xs = sel => [...document.querySelectorAll(sel)].map(e => Math.round(e.getBoundingClientRect().x)).join(' ');
        const out = [
            `same name:   covers at x ${xs('.plain .cover')}, tips at x ${xs('.plain .tip')}`,
            `anchor-scope: covers at x ${xs('.scoped .cover')}, tips at x ${xs('.scoped .tip')}`,
            `anchor outside the containing block: tip ${box(document.getElementById('lost'))}, box ${box(document.querySelector('.box'))}`,
        ];
        const scroller = document.getElementById('scroller');
        const tip = document.getElementById('follow');
        for (const top of [0, 40, 200]) {
            scroller.scrollTop = top;
            await frame();
            const r = tip.getBoundingClientRect();
            const hit = document.elementFromPoint(r.x + 5, r.y + 5);
            out.push(`scrollTop ${String(top).padStart(3)}: anchor ${box(document.getElementById('scrolled'))}, tip ${box(tip)}, ` +
                `hit test at the tip finds ${hit === tip ? 'the tip' : 'something else'}`);
        }
        const spellings = ['anchors-visible', 'anchor-visible'].map(v => `${v} ${CSS.supports('position-visibility', v)}`);
        out.push(`position-visibility: ${spellings.join(', ')}, computed "${getComputedStyle(tip).positionVisibility}"`);
        return out.join('\n');
    }, [`(${box})`, `(${frame})`]));
    await browser.close();

    // 3) Bookstore: the user menu and a book preview, both popovers opened by invoker commands
    // (reduced motion: the cards' scroll-driven slide would move the anchor while we measure)
    ({ browser, page } = await open('catalog.html', { engine, width: 1280, height: 600, reducedMotion: 'reduce' }));
    lines.push(await page.evaluate(async ([boxSource, frameSource]) => {
        const [box, frame] = [eval(boxSource), eval(frameSource)];
        const out = [];
        const user = document.querySelector('.user-button');
        user.click();
        out.push(`user menu: button ${box(user)}, menu ${box(document.getElementById('user-menu'))}`);
        const title = document.querySelector('[commandfor="preview-dune"]');
        const preview = document.getElementById('preview-dune');
        // room below the list, so the title can be scrolled close to the top of the window
        document.body.style.paddingBlockEnd = '600px';
        for (const y of [450, 90]) {
            // put the title at y px from the top of the window, then open the preview
            scrollTo({ top: scrollY + title.getBoundingClientRect().y - y, behavior: 'instant' });
            await frame();
            title.click();
            await frame();
            const [t, p] = [title.getBoundingClientRect(), preview.getBoundingClientRect()];
            out.push(`title at y ${Math.round(t.y)}: preview ${box(preview)}, ${p.bottom <= t.top ? 'above' : 'below'} the title`);
            title.click();
        }
        return out.join('\n');
    }, [`(${box})`, `(${frame})`]));
    await browser.close();
    results[engine] = lines.join('\n');
}
console.log(`--- chromium\n${results.chromium}`);
console.log(`--- webkit: ${results.webkit === results.chromium ? 'the same values' : `\n${results.webkit}`}`);
