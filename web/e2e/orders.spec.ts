import { expect, test } from './fixtures';

test('the order page shows a seeded order', { tag: '@smoke' }, async ({ placedOrder, page }) => {
  await test.step('open the order page', async () => {
    await page.goto(`/orders/${placedOrder.id}`);
    await expect(page.getByRole('heading', { name: 'Order placed' })).toBeVisible();
  });

  await test.step('check the summary', async () => {
    await expect(page.getByText(placedOrder.reference)).toBeVisible();
    await expect(page.getByRole('definition').nth(1)).toShowPrice(48.75);
    await expect(page.getByRole('definition').nth(2)).toHaveText('TRK-E2E-1');
  });
});
