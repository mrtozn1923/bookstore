// Font lab: which font really draws Turkish text, how many font bytes are downloaded, and how much
// layout shift the font swap causes. The page is served by Playwright itself (http://fontlab.test).
// Usage: node font-check.mjs
import { readFile } from 'node:fs/promises';
import { chromium } from 'playwright';
import { chromiumArgs } from './browser.mjs';

const appFonts = new URL('../bookstore-web/public/fonts/', import.meta.url);
const labFonts = new URL('./fonts/', import.meta.url);
const LATIN = 'U+0000-00FF, U+0131, U+0152-0153, U+02BB-02BC, U+02C6, U+02DA, U+02DC, U+0304, U+0308, U+0329, U+2000-206F, U+20AC, U+2122, U+2191, U+2193, U+2212, U+2215, U+FEFF, U+FFFD';
const LATIN_EXT = 'U+0100-02BA, U+02BD-02C5, U+02C7-02CC, U+02CE-02D7, U+02DD-02FF, U+0304, U+0308, U+0329, U+1D00-1DBF, U+1E00-1E9F, U+1EF2-1EFF, U+2020, U+20A0-20AB, U+20AD-20C0, U+2113, U+2C60-2C7F, U+A720-A7FF';
const face = (file, weight, range) =>
  `@font-face { font-family: 'Inter'; font-weight: ${weight}; font-display: swap; src: url('/fonts/${file}') format('woff2');${range ? ` unicode-range: ${range};` : ''} }`;
const fallback = `@font-face { font-family: 'Inter Fallback'; src: local('Arial');
  size-adjust: 105.53%; ascent-override: 91.80%; descent-override: 22.86%; line-gap-override: 0%; }`;

const scenarios = {
  'latin only': { css: [face('inter-latin-400-normal.woff2', 400, LATIN), face('inter-latin-700-normal.woff2', 700, LATIN)], family: 'Inter, Arial' },
  'latin + latin-ext': {
    css: [400, 700].flatMap((w) => [face(`inter-latin-${w}-normal.woff2`, w, LATIN), face(`inter-latin-ext-${w}-normal.woff2`, w, LATIN_EXT)]),
    family: 'Inter, Arial',
  },
  'tr subset': { css: [face('inter-tr-400.woff2', 400), face('inter-tr-700.woff2', 700)], family: 'Inter, Arial' },
  'tr subset + fallback metrics': {
    css: [face('inter-tr-400.woff2', 400), face('inter-tr-700.woff2', 700), fallback],
    family: "Inter, 'Inter Fallback', Arial",
  },
};

const page = (s) => `<!doctype html><html lang="tr"><meta charset="utf-8"><meta name="viewport" content="width=device-width">
<style>${s.css.join('\n')}
body { margin: 16px; font-family: ${s.family}; } h1 { font-size: 30px; } .box { height: 120px; background: #e2e8f0; }</style>
<h1>Haftanın kitabı: Dune</h1>
<p id="text">Kitaplığımızda Şubat ayında İstanbul'dan gelen yeni çıkanlar var. Işıklı vitrinde ağır ciltli,
özel basım Dune ile çağdaş klasikler yan yana; sipariş ödemesi güvenli, kargo hızlı ve ücretsiz.</p>
<p>Okul ve kütüphaneler için kurumsal siparişte en az 20 adet, %10 indirim ve kısmi sevkiyat seçeneği var.
Öğretmenler sınıf listesini yükleyip her öğrenciye ayrı paket hazırlatabiliyor.</p>
<p>Kargo takibi, iade talebi ve fatura bilgileri sipariş geçmişinde. Sorularınız için müşteri hizmetleri
hafta içi her gün 09.00 ile 18.00 arasında çevrimiçi sohbette.</p>
<div class="box">Sepete ekle</div></html>`;

const browser = await chromium.launch({ args: chromiumArgs });
console.log('scenario                       font bytes   glyphs drawn for <p>                  CLS');
for (const [name, scenario] of Object.entries(scenarios)) {
  const context = await browser.newContext({ viewport: { width: 412, height: 915 }, deviceScaleFactor: 2.625 });
  const tab = await context.newPage();
  let fontBytes = 0;
  await tab.route('http://fontlab.test/**', async (route) => {
    const path = new URL(route.request().url()).pathname;
    if (path === '/') return route.fulfill({ contentType: 'text/html; charset=utf-8', body: page(scenario) });
    const file = path.replace('/fonts/', '');
    const body = await readFile(new URL(file, file.startsWith('inter-tr') ? labFonts : appFonts));
    fontBytes += body.length;
    // A font that arrives 800 ms after the text has been painted with the fallback font
    await new Promise((r) => setTimeout(r, 800));
    return route.fulfill({ contentType: 'font/woff2', body });
  });
  await tab.addInitScript(() => {
    window.__cls = 0;
    new PerformanceObserver((list) => {
      for (const e of list.getEntries()) if (!e.hadRecentInput) window.__cls += e.value;
    }).observe({ type: 'layout-shift', buffered: true });
  });
  await tab.goto('http://fontlab.test/');
  await tab.waitForTimeout(2500);
  const cdp = await context.newCDPSession(tab);
  await cdp.send('DOM.enable');
  await cdp.send('CSS.enable');
  const { root } = await cdp.send('DOM.getDocument');
  const { nodeId } = await cdp.send('DOM.querySelector', { nodeId: root.nodeId, selector: '#text' });
  const { fonts } = await cdp.send('CSS.getPlatformFontsForNode', { nodeId });
  const drawn = fonts.map((f) => `${f.familyName} ${f.glyphCount}`).join(', ');
  const cls = await tab.evaluate(() => window.__cls.toFixed(4));
  console.log(`${name.padEnd(30)} ${`${(fontBytes / 1024).toFixed(1)} KB`.padStart(10)}   ${drawn.padEnd(36)} ${cls}`);
  await context.close();
}
await browser.close();
