import { chromium } from 'playwright';
import { serve } from './serve.mjs';

// Opens one Chromium page against the local server and always cleans up.
export async function withPage(fn, { width = 1280, height = 800, scale = 1, colorScheme = 'light' } = {}) {
  const server = await serve();
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage({ viewport: { width, height }, deviceScaleFactor: scale, colorScheme });
    const open = async (path) => {
      await page.goto(`${server.base}/${path}`);
      await page.evaluate(() => document.fonts.ready);
    };
    return await fn({ page, open, browser });
  } finally {
    await browser.close();
    server.close();
  }
}
