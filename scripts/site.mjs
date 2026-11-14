import { chromium, webkit } from 'playwright';
import { readFile } from 'node:fs/promises';
import path from 'node:path';

// serves web/ under a made-up http origin: cross-document view transitions work only between same-origin pages
const types = { '.html': 'text/html', '.js': 'text/javascript', '.css': 'text/css', '.svg': 'image/svg+xml' };

export async function openSite(file, { engine = 'chromium', width = 1280, height = 800, ...context } = {}) {
    const browser = await (engine === 'webkit' ? webkit : chromium).launch();
    const page = await browser.newPage({ viewport: { width, height }, ...context });
    await page.route('http://bookstore.test/**', async route => {
        const name = new URL(route.request().url()).pathname;
        const body = await readFile(path.join('web', name)).catch(() => null);
        await (body ? route.fulfill({ body, contentType: types[path.extname(name)] ?? 'application/octet-stream' })
            : route.fulfill({ status: 404 }));
    });
    await page.goto(`http://bookstore.test/${file}`);
    return { browser, page };
}
