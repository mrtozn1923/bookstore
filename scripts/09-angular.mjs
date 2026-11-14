import { chromium, webkit } from 'playwright';
import { readFile } from 'node:fs/promises';
import path from 'node:path';

// serves angular/dist/bookstore-web/browser under a made-up origin, no web server needed
const dist = path.resolve('angular/dist/bookstore-web/browser');
const types = { '.html': 'text/html', '.js': 'text/javascript', '.css': 'text/css', '.svg': 'image/svg+xml' };

for (const engine of [chromium, webkit]) {
    const browser = await engine.launch();
    const page = await browser.newPage({ viewport: { width: 1024, height: 600 } });
    await page.route('http://bookstore.test/**', async route => {
        const file = new URL(route.request().url()).pathname.replace(/^\/$/, '/index.html');
        const body = await readFile(path.join(dist, file)).catch(() => null);
        await (body ? route.fulfill({ body, contentType: types[path.extname(file)] ?? 'application/octet-stream' })
            : route.fulfill({ status: 404 }));
    });
    await page.goto('http://bookstore.test/');
    await page.waitForSelector('app-book-card h3');
    console.log(`--- ${engine.name()}`);
    console.log(await page.evaluate(() => {
        const lines = [...document.querySelectorAll('app-book-card')].map(host => {
            const card = host.querySelector('.card');
            return `${host.querySelector('h3').textContent.padEnd(5)} host w=${Math.round(host.getBoundingClientRect().width)} ` +
                `container-type ${getComputedStyle(host).containerType}, .card columns "${getComputedStyle(card).gridTemplateColumns}"`;
        });
        // the :host rule after Angular's emulated encapsulation rewrote it
        const rules = [...document.styleSheets].flatMap(s => [...s.cssRules]).map(r => r.cssText);
        lines.push(rules.find(t => t.startsWith('[_nghost')));
        lines.push(rules.find(t => t.startsWith('@container')).replace(/\s+/g, ' '));
        return lines.join('\n');
    }));
    await browser.close();
}
