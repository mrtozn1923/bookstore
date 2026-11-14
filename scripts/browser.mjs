import { chromium, webkit } from 'playwright';
import { pathToFileURL } from 'node:url';
import path from 'node:path';

export const pageUrl = (file) => pathToFileURL(path.resolve('web', file)).href;

// extra options (deviceScaleFactor, isMobile, hasTouch, reducedMotion, contrast, colorScheme…) go to the page context
export async function open(file, { engine = 'chromium', width = 1280, height = 800, ...context } = {}) {
    const browser = await (engine === 'webkit' ? webkit : chromium).launch();
    const page = await browser.newPage({ viewport: { width, height }, ...context });
    await page.goto(pageUrl(file));
    return { browser, page };
}
