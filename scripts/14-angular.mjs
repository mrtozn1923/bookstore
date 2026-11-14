import { openAngular } from './angular-page.mjs';

// withViewTransitions(): which ::view-transition groups does a router navigation create?
for (const engine of ['chromium', 'webkit']) {
    for (const reducedMotion of ['no-preference', 'reduce']) {
        const { browser, page } = await openAngular(engine, { reducedMotion });
        const groups = await page.evaluate(async () => {
            const ready = new Promise(resolve => {
                const original = document.startViewTransition.bind(document);
                document.startViewTransition = update => { const t = original(update); t.ready.then(resolve); return t; };
            });
            [...document.querySelectorAll('app-book-card a')].find(a => a.textContent === 'Dune').click();
            await ready;
            return document.getAnimations()
                .filter(a => a.effect.pseudoElement?.startsWith('::view-transition-group'))
                .map(a => {
                    const [from, to] = [a.effect.getKeyframes()[0], a.effect.getKeyframes().at(-1)];
                    return `${a.effect.pseudoElement} ${a.effect.getComputedTiming().duration}ms ` +
                        `${from.width} x ${from.height} -> ${to.width} x ${to.height}`;
                }).sort().join('\n  ');
        });
        console.log(`--- ${engine} ${reducedMotion}, ${new URL(page.url()).pathname}\n  ${groups}`);
        await browser.close();
    }
}
