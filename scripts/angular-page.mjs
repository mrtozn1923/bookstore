import { chromium, webkit } from 'playwright';
import { readFile } from 'node:fs/promises';
import path from 'node:path';

// opens the built Angular app (angular/dist) under a made-up origin, no web server needed
const dist = path.resolve('angular/dist/bookstore-web/browser');
const types = { '.html': 'text/html', '.js': 'text/javascript', '.css': 'text/css', '.svg': 'image/svg+xml' };

export async function openAngular(engine = 'chromium', { width = 1024, height = 600, ...context } = {}) {
    const browser = await (engine === 'webkit' ? webkit : chromium).launch();
    const page = await browser.newPage({ viewport: { width, height }, ...context });
    await page.route('http://bookstore.test/**', async route => {
        // an unknown path without a file extension is a client-side route: answer with index.html
        const pathname = new URL(route.request().url()).pathname;
        const file = path.extname(pathname) ? pathname : '/index.html';
        const body = await readFile(path.join(dist, file)).catch(() => null);
        await (body ? route.fulfill({ body, contentType: types[path.extname(file)] ?? 'application/octet-stream' })
            : route.fulfill({ status: 404 }));
    });
    await page.goto('http://bookstore.test/');
    await page.waitForSelector('app-book-card h3');
    return { browser, page };
}
