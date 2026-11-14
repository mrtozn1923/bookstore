import { withPage } from './lib/browser.mjs';

// Spacing audit: which margin/padding/gap values are used, card padding vs gap, borders and text edges.
function audit() {
  const values = new Set();
  let borders = 0;
  for (const el of document.querySelectorAll('main, main *')) {
    const cs = getComputedStyle(el);
    // horizontal margins are left out: here they are only auto (centering)
    for (const p of ['marginTop', 'marginBottom', 'paddingTop', 'paddingBottom', 'paddingLeft', 'paddingRight', 'rowGap', 'columnGap']) {
      const v = parseFloat(cs[p]);
      if (v > 0) values.add(v);
    }
    if (['Top', 'Right', 'Bottom', 'Left'].some((s) => parseFloat(cs[`border${s}Width`]) > 0) && el.tagName !== 'BUTTON') borders++;
  }
  const card = document.querySelector('.card');
  const cardBox = card.getBoundingClientRect();
  // left edge of what the eye sees: the box for the image and button, the first glyph for text
  const edges = new Set([...card.querySelectorAll('img, .title, .author, .price, .btn')].map((e) => {
    const r = document.createRange();
    r.selectNodeContents(e);
    const box = e.matches('img, .btn') ? e.getBoundingClientRect() : r.getClientRects()[0];
    return Math.round(box.left - cardBox.left);
  }));
  const second = document.querySelectorAll('.card')[1].getBoundingClientRect();
  return {
    values: [...values].sort((a, b) => a - b),
    padding: parseFloat(getComputedStyle(card).paddingLeft),
    gap: Math.round(second.left - cardBox.right),
    borders,
    edges: [...edges].sort((a, b) => a - b),
    pageWidth: document.documentElement.scrollWidth,
    columns: getComputedStyle(document.querySelector('.grid')).gridTemplateColumns.split(' ').length,
    cardHeight: Math.round(cardBox.height),
  };
}

await withPage(async ({ page, open }) => {
  for (const [variant, density] of [['before'], ['after'], ['after', 'compact']]) {
    for (const width of [1280, 390]) {
      await page.setViewportSize({ width, height: 800 });
      await open(`web/05-spacing/${variant}.html`);
      if (density) await page.evaluate((d) => (document.documentElement.dataset.density = d), density);
      const a = await page.evaluate(audit);
      const name = `${variant}${density ? ' ' + density : ''}`;
      if (width === 1280) {
        const offGrid = a.values.filter((v) => v % 4 !== 0);
        console.log(`--- ${name}: spacing values ${a.values.join('/')} (${offGrid.length} not on the 4px grid)`);
        console.log(`  card padding ${a.padding}px, gap between cards ${a.gap}px, bordered elements ${a.borders}, text edges in a card ${a.edges.join('/')}`);
      }
      console.log(`  @${width}: ${a.columns} columns, card ${a.cardHeight}px tall, page ${a.pageWidth}px wide${a.pageWidth > width ? ' (horizontal scroll)' : ''}`);
    }
  }
});
