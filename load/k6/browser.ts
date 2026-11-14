import { browser } from 'k6/browser';
import { expect } from 'https://jslib.k6.io/k6-testing/0.6.1/index.js';

// Browser test: a few real browsers next to the protocol-level load, measuring what the user sees.
// Usage: k6 run browser.ts   (the Angular client must be running: cd web/bookstore-web; npm start)
const WEB_URL = __ENV.WEB_URL || 'http://localhost:4200';

export const options = {
    scenarios: {
        ui: {
            executor: 'shared-iterations',
            vus: 1,
            iterations: 5,
            options: { browser: { type: 'chromium' } },
        },
    },
    thresholds: {
        browser_web_vital_lcp: ['p(75)<2500'],
        browser_web_vital_cls: ['p(75)<0.1'],
    },
};

export default async function () {
    const page = await browser.newPage();
    try {
        await page.goto(WEB_URL);
        // Auto-retrying assertions: wait until the catalog has loaded.
        await expect(page.getByTestId('book-count')).toBeVisible();
        await page.locator('.book-card a').first().click();
        await expect(page.locator('h1')).toBeVisible();
        await page.getByRole('button', { name: 'Add to cart' }).click();
        await expect(page.getByText('Added.')).toBeVisible();
    } finally {
        await page.close();
    }
}
