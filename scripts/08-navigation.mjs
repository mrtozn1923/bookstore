import { withPage } from './lib/browser.mjs';

const where = (page) => {
  const url = new URL(page.url());
  return `${url.pathname.split('/').slice(-2).join('/')}${url.search}`;
};
// active filters as the page shows them: checked boxes plus the current category link
const active = (page) => page.evaluate(() => [
  ...[...document.querySelectorAll('input[type=checkbox]:checked')].map((i) => i.value),
  ...[...document.querySelectorAll('#cats [aria-current]')].map((a) => a.textContent),
].join(', ') || '(none)');

// Opens the 40th book and goes Back; reports whether that book is still in view.
async function reopen(page, open, variant) {
  const path = `web/08-catalog/${variant}.html`;
  await open(path);
  if (variant === 'before') {
    while ((await page.locator('.card').count()) < 40) { await page.mouse.wheel(0, 20000); await page.waitForTimeout(300); }
  } else {
    await page.click('#pages a:text("4")');
  }
  const target = page.locator('.card').nth(variant === 'before' ? 39 : 3);
  const title = await target.locator('.title').textContent();
  await target.scrollIntoViewIfNeeded();
  await target.click();
  await page.waitForLoadState();
  await page.goBack();
  await page.waitForTimeout(500);
  const state = await page.evaluate((t) => {
    const el = [...document.querySelectorAll('.card .title')].find((e) => e.textContent === t);
    const r = el?.getBoundingClientRect();
    return el ? (r.top >= 0 && r.bottom <= innerHeight ? 'in view' : 'on the page, scrolled away') : 'not on the page';
  }, title);
  return `"${title}" ${state}, url ${where(page)}`;
}

for (const variant of ['before', 'after']) {
  const path = `web/08-catalog/${variant}.html`;
  console.log(`--- ${variant}`);
  await withPage(async ({ page, open }) => {
    const pick = async (category, format) => {
      if (variant === 'before') await page.check(`[data-facet=categories][value=${category}]`);
      else await page.click(`#cats a:text("Classics")`);
      await page.check(`input[value=${format}]`);
      await page.waitForTimeout(100);
    };

    // 1) filter, reload, go back
    await open('web/05-spacing/after.html');
    await open(path);
    await pick('classics', 'paperback');
    const count = await page.$eval('#count', (e) => e.textContent).catch(() => '(no count shown)');
    console.log(`  filtered to Classics + Paperback: url ${where(page)}, ${count}`);
    await page.reload();
    await page.waitForTimeout(100);
    console.log(`  after reload: active filters ${await active(page)}`);
    await page.goBack();
    await page.waitForTimeout(200);
    console.log(`  Back: url ${where(page)}, active filters ${page.url().includes('08-catalog') ? await active(page) : '(left the catalog)'}`);

    // 2) can the footer be reached?
    await open(path);
    // scroll to the bottom until the footer is in view and no new books were added by that scroll
    let scrolls = 0;
    while (scrolls < 15) {
      const cards = await page.locator('.card').count();
      await page.mouse.wheel(0, 20000);
      scrolls++;
      await page.waitForTimeout(400);
      const visible = await page.$eval('.footer', (f) => f.getBoundingClientRect().top < innerHeight);
      if (visible && cards === (await page.locator('.card').count())) break;
    }
    console.log(`  footer: in view once ${await page.locator('.card').count()} of ${await page.evaluate(() => books.length)} cards are on the page`);

    // 3) links a crawler can follow inside the list
    const links = await page.$$eval('main a[href]', (as) => as.filter((a) => !a.getAttribute('href').startsWith('#')).length);
    console.log(`  links in the list a crawler can follow: ${links}`);

    // 4) open the 40th book, then come back
    console.log(`  open the 40th book, Back: ${await reopen(page, open, variant)}`);

    // 5) no results: "Austen" in Gothic + Hardcover (after: the same state opened from a shared link)
    if (variant === 'before') {
      await open(path);
      await page.check('[data-facet=categories][value=gothic]');
      await page.check('input[value=hardcover]');
      await page.fill('#q', 'Austen');
      await page.press('#q', 'Enter');
    } else {
      await open(`${path}?category=gothic&format=hardcover&q=Austen`);
    }
    const empty = await page.$eval('#grid', (g) => ({
      text: g.querySelector('h2, p')?.textContent.trim(),
      ways: [...g.querySelectorAll('a')].map((a) => a.closest('li')?.textContent.trim() ?? a.textContent.trim()),
    }));
    console.log(`  "Austen" in Gothic + Hardcover: "${empty.text}", ${empty.ways.length} way(s) forward${empty.ways.length ? `: ${empty.ways.join(' | ')}` : ''}`);
  }, { width: 1100, height: 800 });
}

