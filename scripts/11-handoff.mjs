import { readFile } from 'node:fs/promises';
import { withPage } from './lib/browser.mjs';
import { toCss, declarations } from './lib/autolayout.mjs';

const json = async (path) => JSON.parse(await readFile(path, 'utf8'));
const node = await json('web/11-handoff/cart-bar.figma.json');
const { variables, variableCollections } = (await json('web/11-handoff/variables.figma.json')).meta;

// 1) the CSS that the Auto Layout settings describe
console.log('--- Auto Layout -> CSS');
const unbound = [];
const walk = (n, parent) => {
  console.log(`${n.name}: ${declarations(toCss(n, variables, parent, unbound).css)}`);
  for (const child of n.children ?? []) walk(child, n);
};
walk(node, { layoutMode: 'HORIZONTAL' });
console.log(`values not bound to a variable: ${unbound.join(', ')}`);

// 2) the same bar built from Auto Layout and from copied pixel values, at two widths and in dark mode
console.log('--- rendered');
for (const [label, path] of [['auto layout', 'web/11-handoff/index.html'], ['copied values', 'web/11-handoff/copied.html']]) {
  for (const [width, scheme] of [[390, 'light'], [768, 'light'], [390, 'dark']]) {
    await withPage(async ({ page, open }) => {
      await open(path);
      await page.waitForSelector('body[data-ready]', { state: 'attached' });
      const r = await page.evaluate(() => {
        const box = (name) => document.querySelector(`[data-name="${name}"]`).getBoundingClientRect();
        const bar = box('CartBar');
        const button = box('Checkout');
        const bg = getComputedStyle(document.querySelector('[data-name="CartBar"]')).backgroundColor;
        return { bar: Math.round(bar.width), gap: Math.round(innerWidth - button.right), button: `${Math.round(button.width)}x${Math.round(button.height)}`, bg };
      });
      const line = scheme === 'dark' ? `dark: bar background ${r.bg}` : `bar ${r.bar} px wide, checkout ${r.button}, ${r.gap} px from the right edge`;
      console.log(`  ${label} at ${width} px: ${line}`);
    }, { width, height: 700, colorScheme: scheme });
  }
}

// 3) Figma variables against the DTCG tokens: same names, same values?
console.log('--- variables vs tokens');
const tokens = {
  light: [await json('tokens/primitive.tokens.json'), await json('tokens/semantic.tokens.json')],
  dark: [await json('tokens/primitive.tokens.json'), await json('tokens/semantic.tokens.json'), await json('tokens/theme-dark.tokens.json')],
};
const lookup = (layers, path) => {
  let found;
  for (const layer of layers) {
    const hit = path.split('.').reduce((o, k) => o?.[k], layer);
    if (hit?.$value !== undefined) found = hit.$value;
  }
  return found;
};
const tokenHex = (layers, path) => {
  const v = lookup(layers, path);
  if (v === undefined) return undefined;
  if (typeof v === 'string') return tokenHex(layers, v.slice(1, -1));
  return v.hex;
};
const figmaHex = (id, modeIndex) => {
  const v = variables[id];
  const modes = variableCollections[v.variableCollectionId].modes;
  const value = v.valuesByMode[modes[Math.min(modeIndex, modes.length - 1)].modeId];
  if (value.type === 'VARIABLE_ALIAS') return figmaHex(value.id, modeIndex);
  return '#' + [value.r, value.g, value.b].map((c) => Math.round(c * 255).toString(16).padStart(2, '0')).join('');
};
// Figma "blue/600" -> token "palette.blue.600", "color/action" -> "color.action"
const tokenPath = (name) => (name.startsWith('color/') ? name : `palette/${name}`).replaceAll('/', '.');
const colors = Object.values(variables).filter((v) => v.resolvedType === 'COLOR');
const onlyFigma = colors.filter((v) => tokenHex(tokens.light, tokenPath(v.name)) === undefined).map((v) => v.name);
const both = colors.filter((v) => !onlyFigma.includes(v.name));
const drift = [];
for (const v of both) {
  ['light', 'dark'].forEach((mode, i) => {
    const code = tokenHex(tokens[mode], tokenPath(v.name));
    const design = figmaHex(v.id, i);
    if (code !== design && !(v.variableCollectionId.endsWith('1:0') && i === 1)) drift.push(`${v.name} (${mode}) Figma ${design}, code ${code}`);
  });
}
const semanticInCode = Object.keys(tokens.light[1].color).map((k) => `color/${k}`);
const onlyCode = semanticInCode.filter((n) => !colors.some((v) => v.name === n));
console.log(`colour variables: ${colors.length}, in both: ${both.length}, only in Figma: ${onlyFigma.join(', ')}`);
console.log(`different values: ${drift.length ? drift.join('; ') : 'none'}`);
console.log(`semantic colours only in code: ${onlyCode.length} (${onlyCode.slice(0, 4).join(', ')}, ...)`);
