import { open } from './browser.mjs';
import { openSite } from './site.mjs';

// the ::view-transition-* animations running right after the transition has started
const groups = () => document.getAnimations()
    .filter(a => a.effect.pseudoElement?.startsWith('::view-transition-group'))
    .map(a => {
        const [from, to] = [a.effect.getKeyframes()[0], a.effect.getKeyframes().at(-1)]
            .map(k => new DOMMatrix(k.transform));
        return { duration: a.effect.getComputedTiming().duration, dx: to.e - from.e, dy: to.f - from.f };
    });

for (const engine of ['chromium', 'webkit']) {
    console.log(`--- ${engine}`);

    // 1) same-document: sorting the catalog by price
    for (const reducedMotion of ['no-preference', 'reduce']) {
        // a tall window: every card is fully in view, so the scroll-driven reveal has finished
        const { browser, page } = await open('catalog.html', { engine, reducedMotion, height: 1200 });
        const titles = () => page.$$eval('.book-list .book-title', h => h.map(x => x.textContent).join(', '));
        const before = await titles();
        const moves = await page.evaluate(async groupsSource => {
            const groups = eval(groupsSource);
            const select = document.querySelector('.catalog-toolbar select');
            select.value = 'Price';
            const started = new Promise(resolve => {
                const original = document.startViewTransition.bind(document);
                document.startViewTransition = update => { const t = original(update); t.ready.then(resolve); return t; };
            });
            select.dispatchEvent(new Event('change'));
            await started;
            // sorted, because engines list the animations in different orders
            return groups().sort((a, b) => b.duration - a.duration || a.dx - b.dx).map(g => `${g.duration}ms (${Math.round(g.dx)}, ${Math.round(g.dy)})`).join(' ');
        }, `(${groups})`);
        console.log(`sort ${reducedMotion}: ${before} -> ${await titles()}\n  groups: ${moves}`);
        await browser.close();
    }

    // 2) a duplicate view-transition-name
    let { browser, page } = await open('lab/14-view-transitions.html', { engine });
    console.log(`duplicate name: ${await page.evaluate(async () => {
        // the update itself still runs; only the animation is skipped
        const transition = document.startViewTransition(() => { document.body.dataset.updated = 'yes'; });
        const result = await transition.ready.then(() => 'ran', e => `${e.name}: ${e.message}`);
        await transition.updateCallbackDone;
        return `${result} | update ran: ${document.body.dataset.updated}`;
    })}`);
    await browser.close();

    // 3) cross-document: catalog -> book page on an http origin
    ({ browser, page } = await openSite('catalog.html', { engine }));
    await page.addInitScript(() => addEventListener('pagereveal', e => {
        window.revealed = e.viewTransition ? 'with a view transition' : 'without a view transition';
    }));
    await Promise.all([page.waitForURL(/book\.html/), page.click('.featured .book-title a')]);
    // pagereveal fires at the first rendering opportunity, which can come after the load event
    await page.waitForFunction(() => window.revealed, null, { timeout: 5000 }).catch(() => { });
    console.log(`book.html revealed ${await page.evaluate(() => window.revealed ?? '(no pagereveal event)')}`);
    await browser.close();
}
