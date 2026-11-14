import { open } from './browser.mjs';

// state of a dialog 100ms into its opening and closing transitions
const probe = async (page, opener, closer, id) => page.evaluate(async ({ opener, closer, id }) => {
    const dialog = document.getElementById(id);
    const at100 = () => {
        const animations = document.getAnimations();
        animations.forEach(a => { a.pause(); a.currentTime = 100; });
        const cs = getComputedStyle(dialog);
        const text = `${String(animations.length).padStart(1)} transitions, opacity ${Number(cs.opacity).toFixed(2)}, ` +
            `translate ${cs.translate}, display ${cs.display}, backdrop ${getComputedStyle(dialog, '::backdrop').backgroundColor}`;
        animations.forEach(a => a.finish());
        return text;
    };
    opener ? document.querySelector(opener).click() : dialog.showModal();
    const opening = at100();
    closer ? document.querySelector(closer).click() : dialog.close();
    const closing = at100();
    return `open  ${opening}\n  close ${closing}`;
}, { opener, closer, id });

for (const engine of ['chromium', 'webkit']) {
    console.log(`--- ${engine}`);
    const { browser, page } = await open('lab/13-dialog.html', { engine });
    for (const id of ['plain', 'starting', 'discrete']) console.log(`${id}\n  ${await probe(page, null, null, id)}`);
    await browser.close();

    // Bookstore: the header button and the dialog's own close button use invoker commands, no script
    for (const reducedMotion of ['no-preference', 'reduce']) {
        const { browser, page } = await open('catalog.html', { engine, reducedMotion });
        console.log(`catalog, ${reducedMotion}\n  ${await probe(page, '.cart-link', '.cart-dialog [command="close"]', 'cart-dialog')}`);
        await browser.close();
    }
}
