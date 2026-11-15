import { expect, test, type Page } from '@playwright/test';

const analyticsLoaded = (page: Page) => page.evaluate(() => '__analyticsStubLoaded' in window);
const consentUpdates = (page: Page) =>
  page.evaluate(() => (window as any).dataLayer.filter((a: any) => a[0] === 'consent').map((a: any) => [a[1], a[2].analytics_storage]));

test('nothing optional runs before the visitor decides', async ({ page, context }) => {
  const scripts: string[] = [];
  page.on('request', r => r.resourceType() === 'script' && scripts.push(new URL(r.url()).pathname));
  await page.goto('/');

  await expect(page.getByRole('dialog', { name: 'Cookie consent' })).toBeVisible();
  expect(scripts).not.toContain('/analytics-stub.js');
  expect(await analyticsLoaded(page)).toBe(false);
  expect((await context.cookies()).map(c => c.name)).toEqual([]);
  expect(await consentUpdates(page)).toEqual([['default', 'denied']]);
});

test('accept, reject and preferences look the same', async ({ page }) => {
  await page.goto('/');
  const look = (name: string) =>
    page.getByRole('button', { name }).evaluate(b => {
      const s = getComputedStyle(b);
      const r = b.getBoundingClientRect();
      return { width: Math.round(r.width), height: Math.round(r.height), font: s.fontSize, color: s.color, background: s.backgroundColor };
    });
  const accept = await look('Accept all');
  expect(await look('Reject all')).toEqual(accept);
  expect(await look('Preferences')).toEqual(accept);
});

test('reject keeps analytics off and records the choice', async ({ page, context }) => {
  await page.goto('/');
  const recorded = page.waitForResponse(r => r.url().endsWith('/api/consents'));
  await page.getByRole('button', { name: 'Reject all' }).click();

  expect((await recorded).status()).toBe(204);
  await expect(page.getByRole('dialog')).toBeHidden();
  expect(await analyticsLoaded(page)).toBe(false);
  const cookie = (await context.cookies()).find(c => c.name === 'bookstore_consent');
  expect(decodeURIComponent(cookie!.value)).toMatch(/^2026-11\|0\|0\|/);
  expect(await consentUpdates(page)).toEqual([['default', 'denied'], ['update', 'denied']]);
});

test('preference boxes start unticked and withdrawal works from the corner button', async ({ page, context }) => {
  await page.goto('/');
  await page.getByRole('button', { name: 'Preferences' }).click();
  await expect(page.getByLabel('Analytics')).not.toBeChecked();
  await expect(page.getByLabel('Marketing')).not.toBeChecked();

  await page.getByLabel('Analytics').check();
  await page.getByRole('button', { name: 'Save choices' }).click();
  await expect.poll(() => analyticsLoaded(page)).toBe(true);
  await expect.poll(async () => (await context.cookies()).some(c => c.name === '_stub_aid')).toBe(true);

  // Withdrawing must be as easy as giving: one small button, always on the page.
  await page.getByRole('button', { name: 'Cookie settings' }).click();
  await expect(page.getByLabel('Analytics')).toBeChecked();
  await page.getByLabel('Analytics').uncheck();
  await page.getByRole('button', { name: 'Save choices' }).click();

  await expect.poll(async () => (await context.cookies()).some(c => c.name === '_stub_aid')).toBe(false);
  expect((await consentUpdates(page)).at(-1)).toEqual(['update', 'denied']);
  await page.reload();
  expect(await analyticsLoaded(page)).toBe(false);
  await expect(page.getByRole('dialog')).toBeHidden();
});
