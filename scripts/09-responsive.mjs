import { withPage } from './lib/browser.mjs';
import { ratio } from './lib/wcag.mjs';

// Reads single pixels from a screenshot by decoding it in the page.
const pixels = (page, png, points) => page.evaluate(async ([b64, points]) => {
  const bitmap = await createImageBitmap(await (await fetch(`data:image/png;base64,${b64}`)).blob());
  const ctx = new OffscreenCanvas(bitmap.width, bitmap.height).getContext('2d');
  ctx.drawImage(bitmap, 0, 0);
  return points.map(([x, y]) => [...ctx.getImageData(x, y, 1, 1).data.slice(0, 3)]);
}, [png.toString('base64'), points]);

// WCAG 2.5.8: a target under 24x24 passes only if a 24 px circle on its centre touches no other target or circle.
const targets = (page) => page.$$eval('a, button', (els) => {
  const boxes = els.map((e) => e.getBoundingClientRect()).filter((r) => r.width && r.height);
  const small = (r) => r.width < 24 || r.height < 24;
  const centre = (r) => [r.left + r.width / 2, r.top + r.height / 2];
  const distToRect = ([x, y], r) => Math.hypot(Math.max(r.left - x, 0, x - r.right), Math.max(r.top - y, 0, y - r.bottom));
  const failing = boxes.filter((r) => small(r) && boxes.some((o) => o !== r &&
    (distToRect(centre(r), o) < 12 || (small(o) && Math.hypot(centre(r)[0] - centre(o)[0], centre(r)[1] - centre(o)[1]) < 24))));
  const under = (n) => boxes.filter((r) => r.width < n || r.height < n).length;
  return { total: boxes.length, failing: failing.length, under44: under(44), under48: under(48) };
});

for (const variant of ['before', 'after']) {
  const path = `web/09-mobile/${variant}.html`;
  console.log(`--- ${variant}`);
  await withPage(async ({ page, open, browser }) => {
    // 1) horizontal scrolling at every width from 320 to 1280 px
    await open(path);
    const bad = [];
    for (let w = 320; w <= 1280; w += 10) {
      await page.setViewportSize({ width: w, height: 800 });
      if (await page.evaluate(() => document.documentElement.scrollWidth > innerWidth)) bad.push(w);
    }
    const ranges = bad.reduce((acc, w) => {
      const last = acc.at(-1);
      if (last && w - last[1] === 10) last[1] = w; else acc.push([w, w]);
      return acc;
    }, []).map(([a, b]) => (a === b ? `${a}` : `${a}–${b}`));
    console.log(`  horizontal scroll at ${bad.length} of 97 widths${bad.length ? ` (${ranges.join(', ')} px)` : ''}`);

    // 2) a phone: 390x844, touch, no hover
    const base = new URL(page.url()).origin;
    const phone = await browser.newPage({ viewport: { width: 390, height: 844 }, isMobile: true, hasTouch: true });
    await phone.goto(`${base}/${path}`);
    await phone.evaluate(() => document.fonts.ready);
    const firstScreen = await phone.evaluate(() => {
      const bar = document.querySelector('.cart-bar')?.getBoundingClientRect().top ?? innerHeight;
      const cards = [...document.querySelectorAll('.card')];
      const whole = cards.filter((c) => { const r = c.getBoundingClientRect(); return r.top >= 0 && r.bottom <= bar; }).length;
      const add = cards.filter((c) => Number(getComputedStyle(c.querySelector('.add')).opacity) > 0).length;
      return { hover: matchMedia('(hover: hover)').matches, whole, add, cards: cards.length };
    });
    console.log(`  phone 390x844: hover available ${firstScreen.hover}; whole cards in the first screen ${firstScreen.whole}; "Add to cart" visible without hover on ${firstScreen.add} of ${firstScreen.cards}`);
    const t = await targets(phone);
    console.log(`  targets: ${t.total}, failing WCAG 2.5.8 (24 px) ${t.failing}, under 44 px ${t.under44}, under 48 px ${t.under48}`);
    const checkout = await phone.$eval('.checkout, .cart-link', (e) => {
      const r = e.getBoundingClientRect();
      return `${Math.round(r.width)}x${Math.round(r.height)} px, centre at ${Math.round(((r.top + r.height / 2) / innerHeight) * 100)}% of the screen height`;
    });
    console.log(`  checkout: ${checkout}`);
    await phone.setViewportSize({ width: 375, height: 812 });
    const prices = await phone.$$eval('.price', (els) => els.filter((e) => e.offsetParent).length);
    console.log(`  at 375 px: price visible on ${prices} of 6 cards`);
    await phone.close();

    // 3) dark mode: is the card distinguishable from the page, and what happens to the covers?
    const shots = {};
    for (const scheme of ['light', 'dark']) {
      await page.emulateMedia({ colorScheme: scheme });
      await page.setViewportSize({ width: 390, height: 844 });
      await page.evaluate(() => scrollTo(0, 0));
      const card = await page.$eval('.card', (c) => { const r = c.getBoundingClientRect(); return [r.right - 4, r.bottom - 4]; });
      const cover = await page.$eval('.card img', (i) => { const r = i.getBoundingClientRect(); return [r.left + 6, r.top + 6]; });
      const bgPoint = await page.$eval('.card', (c) => { const r = c.getBoundingClientRect(); return [r.right + 8 < innerWidth ? r.right + 8 : 4, r.top + 10]; });
      const [cardPx, coverPx, bgPx] = await pixels(page, await page.screenshot(), [card, cover, bgPoint].map(([x, y]) => [Math.round(x), Math.round(y)]));
      shots[scheme] = { cardPx, coverPx, bgPx };
    }
    const d = shots.dark;
    const coverChanged = shots.light.coverPx.some((v, i) => Math.abs(v - d.coverPx[i]) > 20);
    console.log(`  dark: page ${d.bgPx}, card ${d.cardPx}, card vs page ${ratio(d.cardPx, d.bgPx).toFixed(2)}:1; cover colours ${coverChanged ? 'changed (inverted)' : 'unchanged'}`);
  }, { width: 1280, height: 800 });
}
