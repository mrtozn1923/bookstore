import { readFile } from 'node:fs/promises';
import type { Page } from '@playwright/test';
import { expect, test } from './fixtures';
import { buyerStorageState } from './support/config';

test.use({ storageState: buyerStorageState });

function orderFile(rows: [string, number][]): { name: string; mimeType: string; buffer: Buffer } {
  const csv = ['isbn,quantity', ...rows.map(([isbn, quantity]) => `${isbn},${quantity}`)].join('\r\n');
  return { name: 'order.csv', mimeType: 'text/csv', buffer: Buffer.from(csv) };
}

async function upload(page: Page, file: ReturnType<typeof orderFile>): Promise<void> {
  // Input, app-file-picker'ın shadow root'unda; getByLabel oraya kendiliğinden iner
  await page.getByLabel('Order file').setInputFiles(file);
  // Dosya işlenince önizleme tablosu, hata ya da ikisi birden çıkar; ayrıntıyı test kendisi kontrol eder
  const processed = page.getByRole('table', { name: 'Order lines' }).or(page.getByRole('alert'));
  await expect(processed.first()).toBeVisible();
}

test('a school uploads a bulk order and downloads the invoice', async ({ data, page }, testInfo) => {
  const emma = await data.book('Emma', 39.5, 50);
  const hobbit = await data.book('The Hobbit', 52, 50);

  await page.goto('/institutional/bulk-order');
  await upload(page, orderFile([[emma.isbn, 20], [hobbit.isbn, 10]]));
  await expect(page.getByText('2 lines, 30 copies, 10% institutional discount')).toBeVisible();

  const widget = page.getByTitle('Delivery slot').contentFrame();
  await widget.getByLabel('Delivery day').selectOption('Wednesday afternoon');
  await widget.getByRole('button', { name: 'Reserve slot' }).click();
  await expect(page.getByText('Delivery slot: Wednesday afternoon')).toBeVisible();

  await page.getByLabel('I accept the terms of sale').check();

  // Handler action'dan ÖNCE bağlanır; dialog açıldığında Playwright onu bekler
  let question = '';
  page.once('dialog', (dialog) => {
    question = dialog.message();
    return dialog.accept();
  });
  await page.getByRole('button', { name: 'Submit order' }).click();
  await expect(page.getByRole('status')).toContainText('submitted. Total: ₺1,179.00');
  expect(question).toBe('Submit a bulk order for 30 copies?');

  const downloading = page.waitForEvent('download');
  await page.getByRole('button', { name: 'Download invoice' }).click();
  const download = await downloading;

  expect(download.suggestedFilename()).toMatch(/^invoice-INS-[0-9A-F]{12}\.pdf$/);
  const path = testInfo.outputPath(download.suggestedFilename());
  await download.saveAs(path);
  expect((await readFile(path)).subarray(0, 5).toString()).toBe('%PDF-');
});

test('an order below 20 copies is rejected before it is sent', async ({ data, page }) => {
  const emma = await data.book('Emma', 39.5, 50);

  await page.goto('/institutional/bulk-order');
  await upload(page, orderFile([[emma.isbn, 12]]));

  await expect(page.getByRole('alert')).toHaveText('Institutional orders need at least 20 copies.');
  await expect(page.getByRole('button', { name: 'Submit order' })).toBeDisabled();
});
