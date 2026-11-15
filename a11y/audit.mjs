// Manual-audit helpers: measures what axe does not check on Bookstore's pages.
// Usage: node audit.mjs
import { chromium } from 'playwright';
import { open, pages } from './lib.mjs';

const browser = await chromium.launch();
const context = await browser.newContext({ viewport: { width: 1280, height: 800 } });
const page = await context.newPage();

for (const name of Object.keys(pages)) {
  await open(page, name);
  const result = await page.evaluate(() => {
    const luminance = rgb => {
      const [r, g, b] = rgb.match(/\d+(\.\d+)?/g).slice(0, 3).map(Number).map(v => {
        v /= 255;
        return v <= 0.04045 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4;
      });
      return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    };
    const ratio = (a, b) => {
      const [hi, lo] = [luminance(a), luminance(b)].sort((x, y) => y - x);
      return Math.floor(((hi + 0.05) / (lo + 0.05)) * 100) / 100;
    };
    // 1.4.11: the border is the only thing that shows where a text field is
    const fields = [...document.querySelectorAll('input:not([type=hidden]), select')].map(el => {
      const style = getComputedStyle(el);
      return `${el.name || el.type || el.tagName.toLowerCase()} ${ratio(style.borderTopColor, 'rgb(255, 255, 255)')}:1`;
    });
    // 1.3.5: fields that collect the user's own data need an autocomplete token
    const personal = [...document.querySelectorAll('input[name=name], input[type=email]')]
      .map(el => `${el.name}=${el.getAttribute('autocomplete') ?? '(none)'}`);
    // 2.5.8: targets under 24x24 CSS px, and whether the spacing exception saves them
    const targets = [...document.querySelectorAll('a[href], button, input, select')]
      .map(el => ({ el, r: el.getBoundingClientRect() })).filter(t => t.r.width > 0);
    const isSmall = t => t.r.width < 24 || t.r.height < 24;
    const center = t => [t.r.left + t.r.width / 2, t.r.top + t.r.height / 2];
    const small = targets.filter(isSmall).map(t => {
      const [cx, cy] = center(t);
      // a 24px circle on the target must not touch another target or another small target's circle
      const crowded = targets.some(o => {
        if (o === t) return false;
        const nx = Math.max(o.r.left, Math.min(cx, o.r.right)), ny = Math.max(o.r.top, Math.min(cy, o.r.bottom));
        const [ox, oy] = center(o);
        return Math.hypot(cx - nx, cy - ny) < 12 || (isSmall(o) && Math.hypot(cx - ox, cy - oy) < 24);
      });
      return { label: (t.el.textContent || t.el.type).trim().slice(0, 20), w: Math.round(t.r.width), h: Math.round(t.r.height), crowded };
    });
    return { title: document.title, h1: document.querySelectorAll('h1').length, fields, personal, small };
  });
  // 1.4.10: at 320 CSS px wide nothing should need horizontal scrolling (data tables are an exception)
  await page.setViewportSize({ width: 320, height: 640 });
  const overflow = await page.evaluate(() => {
    const doc = document.documentElement;
    const wide = [...document.querySelectorAll('main *')].filter(e => e.getBoundingClientRect().right > doc.clientWidth + 1);
    return { scroll: doc.scrollWidth - doc.clientWidth, culprits: [...new Set(wide.map(e => e.closest('table') ? 'table' : e.tagName.toLowerCase()))] };
  });
  await page.setViewportSize({ width: 1280, height: 800 });
  console.log(`--- ${name}`);
  console.log(`  title="${result.title}" h1=${result.h1}`);
  console.log(`  field borders: ${result.fields.join(', ') || '-'}`);
  console.log(`  autocomplete: ${result.personal.join(', ') || '-'}`);
  const smallest = result.small.toSorted((x, y) => x.w * x.h - y.w * y.h)[0];
  const crowded = result.small.filter(s => s.crowded).map(s => s.label);
  console.log(`  targets < 24px: ${result.small.length}` + (smallest ? ` (smallest "${smallest.label}" ${smallest.w}x${smallest.h}),` +
    ` without enough spacing: ${crowded.join(', ') || 'none'}` : ''));
  console.log(`  320px: horizontal overflow ${overflow.scroll}px ${overflow.culprits.join(',')}`);
}
await browser.close();
