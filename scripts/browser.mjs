import { chromium, webkit } from 'playwright';
import { pathToFileURL } from 'node:url';
import path from 'node:path';

export const pageUrl = (file) => pathToFileURL(path.resolve('web', file)).href;

export async function open(file, { engine = 'chromium', width = 1280, height = 800 } = {}) {
    const browser = await (engine === 'webkit' ? webkit : chromium).launch();
    const page = await browser.newPage({ viewport: { width, height } });
    await page.goto(pageUrl(file));
    return { browser, page };
}
