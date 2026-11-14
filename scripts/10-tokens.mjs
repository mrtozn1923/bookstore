import { readFile } from 'node:fs/promises';
import { withPage } from './lib/browser.mjs';
import { ratio } from './lib/wcag.mjs';

// 1) what is in the token files
const files = { primitive: 'primitive', semantic: 'semantic', component: 'component', 'dark theme': 'theme-dark', 'reduced motion': 'motion-reduced' };
const walk = (node, inherited, out) => {
  const type = node.$type ?? inherited;
  if ('$value' in node) return out.push({ type, alias: typeof node.$value === 'string' && node.$value.startsWith('{') });
  for (const [key, child] of Object.entries(node)) if (!key.startsWith('$')) walk(child, type, out);
  return out;
};
for (const [layer, file] of Object.entries(files)) {
  const tokens = walk(JSON.parse(await readFile(`tokens/${file}.tokens.json`, 'utf8')), undefined, []);
  const types = Object.entries(Object.groupBy(tokens, (t) => t.type)).map(([t, list]) => `${t} ${list.length}`).join(', ');
  console.log(`${layer.padEnd(15)} ${String(tokens.length).padStart(3)} tokens, ${tokens.filter((t) => t.alias).length} aliases (${types})`);
}

// Resolves every custom property of a stylesheet to what the browser actually uses ('' = not defined).
function resolveAll(names) {
  const ctx = document.createElement('canvas').getContext('2d', { willReadFrequently: true });
  const root = getComputedStyle(document.documentElement);
  const probe = document.body.appendChild(document.createElement('div'));
  const style = getComputedStyle(probe);
  const as = { '--font-sans': 'font-family', '--ease-out': 'transition-timing-function', '--shadow-raised': 'box-shadow' };
  const read = (name) => {
    if (!root.getPropertyValue(name).trim()) return '';
    probe.removeAttribute('style');
    const bytes = (color) => {
      ctx.fillStyle = color;
      ctx.fillRect(0, 0, 1, 1);
      return [...ctx.getImageData(0, 0, 1, 1).data].slice(0, 3).join(',');
    };
    if (/^--((blue|gray|green|amber|red)-\d+|color-.*)$/.test(name)) {
      probe.style.color = `var(${name})`;
      return bytes(style.color);
    }
    if (name === '--focus-ring') {
      probe.style.outline = `var(${name})`;
      return `${style.outlineWidth} ${style.outlineStyle} ${bytes(style.outlineColor)}`;
    }
    const prop = as[name] ?? (name.startsWith('--weight') ? 'font-weight' : name.startsWith('--leading') ? 'line-height'
      : name.startsWith('--duration') ? 'transition-duration' : 'width');
    if (prop === 'line-height') probe.style.fontSize = '100px';
    probe.style.setProperty(prop, `var(${name})`);
    return style.getPropertyValue(prop);
  };
  const out = Object.fromEntries(names.map((n) => [n, read(n)]));
  probe.remove();
  return out;
}

await withPage(async ({ page }) => {
  const handWritten = await readFile('web/styles/tokens.css', 'utf8');
  const generated = await readFile('web/styles/generated/tokens.css', 'utf8');
  const names = [...new Set(handWritten.match(/--[a-z0-9-]+(?=:)/g))];

  // 2) does the generated CSS give the browser the same values as the hand-written one?
  for (const [label, media] of [['light', { colorScheme: 'light' }], ['dark', { colorScheme: 'dark' }], ['reduced motion', { reducedMotion: 'reduce' }]]) {
    await page.emulateMedia({ colorScheme: 'light', reducedMotion: 'no-preference', ...media });
    const values = [];
    for (const css of [handWritten, generated]) {
      await page.setContent(`<style>${css}</style>`);
      values.push(await page.evaluate(resolveAll, names));
    }
    const missing = names.filter((n) => values[1][n] === '');
    const diff = names.filter((n) => !missing.includes(n) && values[0][n] !== values[1][n]);
    console.log(`${label}: ${names.length - diff.length - missing.length} of ${names.length} identical` +
      `${diff.length ? `; different: ${diff.map((n) => `${n} ${values[0][n]} vs ${values[1][n]}`).join('; ')}` : ''}` +
      `${missing.length ? `; not in the generated file: ${missing.join(', ')}` : ''}`);
  }
}, { width: 800, height: 600 });

// 3) contrast of the semantic pairs, checked once for the whole system
const pairs = [['text', 'bg'], ['text-muted', 'bg'], ['text-muted', 'surface'], ['text-muted', 'surface-raised'], ['link', 'bg'],
  ['on-action', 'action'], ['success-text', 'success-bg'], ['warning-text', 'warning-bg'], ['danger-text', 'danger-bg']];
await withPage(async ({ page, open }) => {
  for (const scheme of ['light', 'dark']) {
    await page.emulateMedia({ colorScheme: scheme });
    await open('web/10-design-system/index.html');
    const rgb = await page.evaluate((pairs) => {
      const ctx = document.createElement('canvas').getContext('2d', { willReadFrequently: true });
      const css = getComputedStyle(document.documentElement);
      const read = (n) => { ctx.fillStyle = css.getPropertyValue(`--color-${n}`); ctx.fillRect(0, 0, 1, 1); return [...ctx.getImageData(0, 0, 1, 1).data].slice(0, 3); };
      return pairs.map(([fg, bg]) => [read(fg), read(bg)]);
    }, pairs);
    const results = pairs.map(([fg, bg], i) => [`${fg}/${bg}`, ratio(...rgb[i])]);
    const low = results.filter(([, r]) => r < 4.5);
    console.log(`contrast ${scheme}: ${results.length - low.length} of ${results.length} pairs at least 4.5:1, lowest ` +
      `${results.toSorted((a, b) => a[1] - b[1])[0].join(' ')}:1${low.length ? `; below: ${low.map((x) => x.join(' ')).join(', ')}` : ''}`);
  }

  // 4) the same token through plain CSS and through Tailwind
  for (const scheme of ['light', 'dark']) {
    await page.emulateMedia({ colorScheme: scheme });
    await open('web/10-design-system/index.html');
    const cssButton = await page.$eval('.btn', (b) => getComputedStyle(b).backgroundColor);
    await open('web/10-design-system/tailwind.html');
    const twButton = await page.$eval('button', (b) => getComputedStyle(b).backgroundColor);
    console.log(`button background ${scheme}: CSS ${cssButton}, Tailwind ${twButton}, same: ${cssButton === twButton}`);
  }
}, { width: 800, height: 600 });
