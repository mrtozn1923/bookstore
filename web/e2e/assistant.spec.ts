import { expect, test, type Page, type Route } from '@playwright/test';
import { DUNE_SHORT } from '../src/testing/recorded-streams';

const QUESTION = 'Which is longer, Dune or The Hobbit? Answer in one sentence.';

function sse(events: [string, unknown][]): string {
  return events.map(([event, data]) => `event: ${event}\ndata: ${JSON.stringify(data)}\n\n`).join('');
}

async function ask(page: Page, text = QUESTION): Promise<void> {
  await page.goto('/');
  await page.getByLabel('Message to the Bookstore assistant').fill(text);
  await page.getByRole('button', { name: 'Send' }).click();
}

test('replays a recorded stream and announces the answer once', async ({ page }) => {
  await page.route('**/chat/sse', (route) =>
    route.fulfill({ status: 200, contentType: 'text/event-stream', body: DUNE_SHORT }),
  );
  await ask(page);
  const answer = page.locator('article').last();
  await expect(answer).toContainText('Dune is longer than The Hobbit');
  await expect(answer).toContainText('AI-generated, may contain mistakes.');
  // 7. yazıdaki erişilebilirlik kuralları artık test: odak input'ta, tek duyuru
  await expect(page.getByLabel('Message to the Bookstore assistant')).toBeFocused();
  await expect(page.locator('.cdk-live-announcer-element')).toHaveText(/^Assistant: Dune is longer than The Hobbit/);
});

test('Stop cancels the request in the browser', async ({ page }) => {
  // Cevap vermiyoruz: model hâlâ "düşünüyor"
  let held: Route | undefined;
  await page.route('**/chat/sse', (route) => {
    held = route;
  });
  const failed = page.waitForEvent('requestfailed', (r) => r.url().endsWith('/chat/sse'));
  await ask(page);
  await page.getByRole('button', { name: 'Stop' }).click();
  expect((await failed).failure()?.errorText).toBe('net::ERR_ABORTED');
  await expect(page.locator('article').last()).toContainText('Stopped.');
  expect(held).toBeDefined();
});

test('a half code block cut by an error still renders as code', async ({ page }) => {
  await page.route('**/chat/sse', (route) =>
    route.fulfill({
      status: 200,
      contentType: 'text/event-stream',
      body: sse([
        ['delta', { text: 'Add the ISBN check:\n\n```csharp\n' }],
        ['delta', { text: 'bool valid = isbn.Length == 13' }],
        ['error', { message: 'The model stopped responding.', partial: true }],
      ]),
    }),
  );
  await ask(page, 'Show me an ISBN check.');
  const answer = page.locator('article').last();
  await expect(answer.locator('pre code')).toContainText('bool valid = isbn.Length == 13');
  await expect(answer).not.toContainText('```');
  await expect(page.getByRole('alert')).toHaveText('The model stopped responding.');
  await expect(answer).toMatchAriaSnapshot(`
    - article:
      - paragraph: "Add the ISBN check:"
      - code: bool valid = isbn.Length == 13
      - paragraph: The answer was cut off.
  `);
});

test('15 seconds of silence becomes a connection error', async ({ page }) => {
  await page.clock.install();
  await page.route('**/chat/sse', () => {
    // hiç cevap yok
  });
  await ask(page);
  await page.clock.fastForward('00:15');
  await expect(page.getByRole('alert')).toHaveText('Connection lost. Check your network and try again.');
});
