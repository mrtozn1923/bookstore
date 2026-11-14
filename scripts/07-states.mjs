import { withPage } from './lib/browser.mjs';
import { comparePng } from './lib/pixels.mjs';

const loaded = (page) => page.waitForSelector('#cart:not([aria-busy]) > *', { timeout: 5000 });

// Tabs through the page; for each stop compares the pixels around the element with and without focus.
async function focusStops(page) {
  const stops = [];
  for (let i = 0; i < 20; i++) {
    await page.keyboard.press('Tab');
    const el = await page.evaluateHandle(() => document.activeElement);
    const name = await el.evaluate((e) => (e === document.body ? null : [...document.querySelectorAll('*')].indexOf(e)));
    if (!name || stops.some((s) => s.name === name)) break;
    const box = await el.boundingBox();
    const clip = { x: Math.max(0, box.x - 6), y: Math.max(0, box.y - 6), width: box.width + 12, height: box.height + 12 };
    const focused = await page.screenshot({ clip });
    await el.evaluate((e) => e.blur());
    const blurred = await page.screenshot({ clip });
    await el.evaluate((e) => e.focus());
    const { changed, strong } = await comparePng(page, focused, blurred);
    // the area of a 2 CSS px thick perimeter of the element (the WCAG 2.4.13 yardstick)
    const perimeter = Math.round(4 * (box.width + box.height) - 16);
    stops.push({ name, changed, strong, perimeter });
  }
  return stops;
}

async function hoverCount(page) {
  const targets = await page.$$('#cart button, #cart a, #search button');
  let changed = 0;
  for (const t of targets) {
    const style = () => t.evaluate((e) => { const cs = getComputedStyle(e); return cs.backgroundColor + cs.color + cs.textDecorationLine; });
    await page.mouse.move(0, 0);
    const before = await style();
    await t.hover();
    await page.waitForTimeout(200);
    if ((await style()) !== before) changed++;
  }
  await page.mouse.move(0, 0);
  return `${changed} of ${targets.length}`;
}

// Clicks "+" on the first row and measures the time until something in the cart changes on screen.
const feedbackMs = (page) => page.evaluate(async () => {
  const box = document.querySelector('#cart');
  const t0 = performance.now();
  const changed = new Promise((resolve) => {
    const mo = new MutationObserver(() => { mo.disconnect(); resolve(performance.now() - t0); });
    mo.observe(box, { subtree: true, childList: true, characterData: true });
  });
  box.querySelector('[data-step="1"]').click();
  return Math.round(await changed);
});

// What a dead-end screen says and how many ways forward it offers.
const describe = (page, sel) => page.$eval(sel, (box) => {
  const actions = [...box.querySelectorAll('a, button, select')].filter((e) => !e.closest('.stepper, tr'));
  const text = (box.querySelector('h2') ?? box.querySelector('p') ?? box).textContent.trim() || '(empty)';
  return `"${text}", ${actions.length} way(s) forward${actions.length ? `: ${actions.map((a) => (a.tagName === 'SELECT' ? 'delivery select' : a.textContent.trim())).join(' | ')}` : ''}`;
});

for (const variant of ['before', 'after']) {
  const path = `web/07-states/${variant}.html`;
  console.log(`--- ${variant}`);
  await withPage(async ({ page, open }) => {
    const dialogs = [];
    page.on('dialog', (d) => { dialogs.push(d.message()); d.accept(); });
    await page.addInitScript(() => {
      window.cls = 0;
      new PerformanceObserver((list) => list.getEntries().forEach((e) => { if (!e.hadRecentInput) window.cls += e.value; }))
        .observe({ type: 'layout-shift', buffered: true });
    });

    // 1) loading
    await open(path);
    await page.waitForTimeout(600);
    const at600 = await page.evaluate(() => {
      const overlay = document.querySelector('#overlay');
      if (overlay && !overlay.hidden) return 'spinner over the whole page';
      return `${document.querySelectorAll('.skeleton-row').length} skeleton rows in the cart's place`;
    });
    await loaded(page);
    await page.waitForTimeout(300);
    console.log(`  loading: at 600 ms ${at600}; layout shift (CLS) ${(await page.evaluate(() => window.cls)).toFixed(3)}`);

    // 2) states of the interactive elements
    const stops = await focusStops(page);
    const visible = stops.filter((s) => s.changed > 0).length;
    const strong = stops.filter((s) => s.strong >= s.perimeter).length;
    console.log(`  focus: ${stops.length} tab stops, visible indicator on ${visible}, a 3:1 change covering a 2 px perimeter on ${strong}`);
    console.log(`  hover: style changes on ${await hoverCount(page)} buttons and links`);
    const step = await page.$eval('[data-step="1"]', (b) => { const r = b.getBoundingClientRect(); return `${r.width}x${r.height}`; });
    const checkout = await page.$eval('.checkout', (b) => (b.disabled ? 'disabled, no reason shown' : 'enabled'));
    console.log(`  "+" button ${step} px; checkout ${checkout}`);

    // 3) feedback after "+"
    console.log(`  "+" clicked: quantity on screen changes after ${await feedbackMs(page)} ms`);
    await page.waitForTimeout(1000);

    // 4) removing a book
    await page.locator('.remove').last().click();
    await page.waitForTimeout(1000);
    const left = await page.locator('[data-id]').count();
    let undo = 'no undo';
    if (await page.locator('#toast button').count()) {
      const message = await page.locator('#toast span').textContent();
      await page.locator('#toast button').click();
      undo = `toast "${message}", Undo brings back ${await page.locator('[data-id]').count() - left} row`;
    }
    console.log(`  remove: dialogs ${dialogs.length ? `"${dialogs.join('", "')}"` : '(none)'}; ${undo}`);
    dialogs.length = 0;

    // 5) dead ends: empty cart, API error, no search results, failed quantity update
    await open(`${path}?cart=empty`);
    await loaded(page);
    console.log(`  empty cart: ${await describe(page, '#cart')}`);
    await open(`${path}?api=fail`);
    await page.waitForTimeout(1500);
    console.log(`  API error: ${dialogs.length ? `alert "${dialogs.pop()}", ` : ''}${await describe(page, '#cart')}`);
    await page.fill('[name=q]', 'dnue');
    await page.press('[name=q]', 'Enter');
    await page.waitForTimeout(800);
    console.log(`  search "dnue": ${await describe(page, '#results')}`);
    await open(`${path}?qty=fail`);
    await loaded(page);
    const errors = [];
    page.on('pageerror', (e) => errors.push(e.message));
    await page.locator('[data-step="1"]').first().click();
    await page.waitForTimeout(1200);
    const shown = await page.locator('.item-error').count() ? await page.locator('.item-error').textContent() : '(nothing)';
    const quantity = await page.locator('[data-id] :is(.count, output)').first().textContent();
    console.log(`  "+" when stock runs out: quantity ${quantity}, message ${shown}${errors.length ? `, uncaught: ${errors[0]}` : ''}`);
  }, { width: 900, height: 900 });
}
