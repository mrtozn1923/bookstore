import { chromium, webkit } from 'playwright';
import { readFile } from 'node:fs/promises';
import path from 'node:path';

// Pages are served from web/ under a fake origin through page.route: no server, no port.
export const ORIGIN = 'http://bookstore.test';
const TYPES = { '.html': 'text/html', '.css': 'text/css', '.js': 'text/javascript', '.svg': 'image/svg+xml',
    '.png': 'image/png', '.webp': 'image/webp', '.avif': 'image/avif', '.woff2': 'font/woff2' };

// extra options (deviceScaleFactor, reducedMotion, forcedColors, colorScheme…) go to the page context
// scrollbars: true shows Chromium's classic scrollbars (headless hides them by default; WebKit on macOS uses overlay ones)
export async function open(file, { engine = 'chromium', width = 1280, height = 800, scrollbars = false, ...context } = {}) {
    const browser = engine === 'webkit'
        ? await webkit.launch()
        : await chromium.launch({ args: ['--use-mock-keychain'], ignoreDefaultArgs: scrollbars ? ['--hide-scrollbars'] : [] });
    const page = await browser.newPage({ viewport: { width, height }, ...context });
    const requests = [];
    await page.route(`${ORIGIN}/**`, async route => {
        const url = new URL(route.request().url());
        requests.push(url.pathname);
        try {
            const body = await readFile(path.join('web', decodeURIComponent(url.pathname)));
            await route.fulfill({ body, contentType: TYPES[path.extname(url.pathname)] ?? 'application/octet-stream' });
        } catch {
            await route.fulfill({ status: 404, body: 'not found' });
        }
    });
    await page.goto(`${ORIGIN}/${file}`);
    await page.evaluate(() => document.fonts.ready);
    return { browser, page, requests };
}

export const engines = ['chromium', 'webkit'];
