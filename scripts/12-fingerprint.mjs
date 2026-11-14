import { open } from './browser.mjs';
import { createHash } from 'node:crypto';

// every computed property of every element in the card list and the sidebar, hashed:
// the same hash before and after a refactoring means the page did not change
for (const engine of ['chromium', 'webkit']) {
    for (const [width, colorScheme] of [[1280, 'light'], [360, 'dark']]) {
        const { browser, page } = await open('catalog.html', { engine, width, colorScheme });
        await page.evaluate(() => document.fonts.ready);
        const styles = await page.evaluate(() => [...document.querySelectorAll('.book-list, .book-list *, .recently-viewed *')]
            .flatMap(el => [null, '::after'].map(pseudo => {
                const cs = getComputedStyle(el, pseudo);
                // sorted: browsers list custom properties in no fixed order
                return [...cs].map(p => `${p}:${cs.getPropertyValue(p)}`).sort().join(';');
            })));
        const hash = createHash('sha256').update(styles.join('\n')).digest('hex').slice(0, 16);
        console.log(`${engine.padEnd(8)} ${String(width).padStart(4)}px ${colorScheme.padEnd(5)} ${styles.length / 2} elements, styles ${hash}`);
        await browser.close();
    }
}
