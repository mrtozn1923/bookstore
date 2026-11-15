// Records what Bookstore tries to tell a screen reader without moving focus: ariaNotify() calls and text changes
// inside live regions. This is a model of the ARIA rules, not a screen reader.
// Usage: node announce.mjs <book|catalog|busy|checkout|order> [no-arianotify]
import { chromium } from 'playwright';
import { base, focusedAx, open } from './lib.mjs';

function recorder() {
  window.__announcements = [];
  const log = entry => window.__announcements.push(entry);
  for (const proto of [Document.prototype, Element.prototype]) {
    const original = proto.ariaNotify;
    if (!original) continue;
    proto.ariaNotify = function (text, options) {
      log(`ariaNotify ${options?.priority ?? 'normal'}: "${text}"`);
      return original.call(this, text, options);
    };
  }
  const liveSelector = '[aria-live]:not([aria-live="off"]), [role="status"], [role="alert"], [role="log"]';
  const politeness = el => el.getAttribute('aria-live') ?? (el.getAttribute('role') === 'alert' ? 'assertive' : 'polite');
  // A region only reports changes if it was already in the DOM before the change
  const known = new WeakSet();
  const lastText = new WeakMap();
  const textOf = el => el.textContent.trim().replace(/\s+/g, ' ');
  const remember = () => document.querySelectorAll(liveSelector).forEach(el => {
    if (!known.has(el)) lastText.set(el, textOf(el));
    known.add(el);
  });
  const observer = new MutationObserver(mutations => {
    const changed = new Set();
    for (const m of mutations) {
      const node = m.target.nodeType === Node.ELEMENT_NODE ? m.target : m.target.parentElement;
      const region = node?.closest(liveSelector);
      if (region) changed.add(region);
      // aria-busy="false" on a container: the regions inside report what changed while it was busy
      if (m.attributeName === 'aria-busy') m.target.querySelectorAll(liveSelector).forEach(el => changed.add(el));
      for (const added of m.addedNodes) {
        if (added.nodeType === Node.ELEMENT_NODE && added.matches(liveSelector) && !known.has(added) && added.textContent.trim()) {
          log(`missed (region inserted with its text): ${added.getAttribute('role') ?? 'aria-live'} "${textOf(added)}"`);
        }
      }
    }
    for (const region of changed) {
      const text = textOf(region);
      if (known.has(region) && text && text !== lastText.get(region) && !region.closest('[aria-busy="true"]')) {
        lastText.set(region, text);
        log(`live ${politeness(region)}${region.getAttribute('aria-atomic') === 'true' ? ' atomic' : ''}: "${text}"`);
      }
    }
    remember();
  });
  document.addEventListener('DOMContentLoaded', () => {
    remember();
    observer.observe(document.body, { subtree: true, childList: true, characterData: true, attributes: true, attributeFilter: ['aria-busy'] });
  });
}

const [mode = 'book', variant] = process.argv.slice(2);
const browser = await chromium.launch();
const context = await browser.newContext();
// "no-arianotify" removes ariaNotify(), like a browser that does not have it yet
if (variant === 'no-arianotify') {
  await context.addInitScript(() => { delete Document.prototype.ariaNotify; delete Element.prototype.ariaNotify; });
}
await context.addInitScript(recorder);
const page = await context.newPage();

async function flush(label) {
  await page.waitForTimeout(400);
  const entries = await page.evaluate(() => window.__announcements.splice(0));
  console.log(`${label}\n  ${entries.join('\n  ') || '(nothing announced)'}\n  focus: ${await focusedAx(page)}`);
}

if (mode === 'book') {
  await open(page, 'book');
  await flush('page loaded');
  await page.getByRole('button', { name: 'Add to cart' }).focus();
  await page.keyboard.press('Enter');
  await page.getByText('Added.').waitFor();
  await flush('Enter on "Add to cart"');
  await page.keyboard.press('Enter');
  await page.getByTestId('cart-link').filter({ hasText: '(2)' }).waitFor();
  await flush('Enter again');
  if (variant === 'no-arianotify') {
    // The CDK's live element must be hidden on screen; that takes the CDK's a11y CSS
    const box = await page.locator('.cdk-live-announcer-element').boundingBox();
    console.log(`.cdk-live-announcer-element on screen: ${box.width}x${box.height} px`);
  }
}

if (mode === 'catalog') {
  await open(page, 'catalog');
  await flush('page loaded');
  await page.getByRole('radio', { name: 'Science Fiction' }).check();
  await page.getByTestId('book-count').filter({ hasText: '3' }).waitFor();
  await flush('category: Science Fiction');
  await page.getByRole('radio', { name: 'All' }).check();
  await page.getByRole('searchbox', { name: 'Search books' }).fill('tolkien');
  await page.keyboard.press('Enter');
  await page.getByTestId('book-count').filter({ hasText: /^1 / }).waitFor();
  await flush('search: tolkien');
  await page.getByRole('searchbox', { name: 'Search books' }).fill('asimov tolkien');
  await page.keyboard.press('Enter');
  await page.getByText('No books match').waitFor();
  await flush('search: asimov tolkien');
}

if (mode === 'busy') {
  // A slow network: every book list answer takes one second
  await open(page, 'catalog');
  await page.route('**/api/books?**', async route => { await new Promise(r => setTimeout(r, 1000)); await route.continue(); });
  await page.evaluate(() => window.__announcements.splice(0));
  const cdp = await context.newCDPSession(page);
  // Live region properties of the status node and its container, as Chromium exposes them
  const props = async () => {
    const { nodes } = await cdp.send('Accessibility.getFullAXTree');
    return nodes.filter(n => ['status', 'generic'].includes(n.role?.value) && n.properties?.some(p => ['live', 'busy'].includes(p.name)))
      .map(n => `${n.role.value} "${n.name?.value ?? ''}" ` + n.properties.filter(p => ['live', 'atomic', 'relevant', 'busy'].includes(p.name))
        .map(p => `${p.name}=${p.value.value}`).join(' ')).join('\n  ');
  };
  await page.getByRole('radio', { name: 'Fantasy' }).check();
  await page.getByText('Loading books…').waitFor();
  console.log(`while loading:\n  ${await props()}`);
  await page.getByTestId('book-count').filter({ hasText: 'Fantasy' }).waitFor();
  console.log(`loaded:\n  ${await props()}`);
  await flush('announcements');
}

if (mode === 'checkout') {
  // War and Peace has 3 in stock; ask for 4 so that the API answers 409
  await page.goto(base + '/books/9781400079988');
  await page.getByRole('spinbutton', { name: 'Quantity' }).fill('4');
  await page.getByRole('button', { name: 'Add to cart' }).click();
  await page.getByTestId('cart-link').filter({ hasText: '(4)' }).waitFor();
  await page.goto(base + '/cart');
  await page.locator('table').waitFor();
  await page.evaluate(() => window.__announcements.splice(0));
  await page.getByLabel('E-mail').fill('ada');
  await page.getByLabel('E-mail').press('Enter');
  await flush('submit: name empty, e-mail "ada"');
  await page.getByLabel('Full name').fill('Ada Lovelace');
  await page.getByLabel('E-mail').fill('ada@example.com');
  await page.getByLabel('E-mail').press('Enter');
  await page.waitForResponse(r => r.url().endsWith('/api/orders'));
  await flush('submit: valid form, 4 x War and Peace (3 in stock)');
}

if (mode === 'order') {
  await open(page, 'cart');
  await page.evaluate(() => window.__announcements.splice(0));
  await page.getByLabel('Full name').fill('Ada Lovelace');
  await page.getByLabel('E-mail').fill('ada@example.com');
  await page.getByLabel('E-mail').press('Enter');
  await page.getByTestId('order-confirmation').waitFor();
  await flush('submit: valid order');
}

await browser.close();
