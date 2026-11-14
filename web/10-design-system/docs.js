// Reads the generated custom properties and renders a swatch for every semantic colour.
const root = getComputedStyle(document.documentElement);
const names = [...document.styleSheets].flatMap((s) => [...s.cssRules])
  .filter((r) => r.selectorText === ':root')
  .flatMap((r) => [...r.style]).filter((n) => n.startsWith('--'));
const unique = [...new Set(names)];

const canvas = document.createElement('canvas').getContext('2d', { willReadFrequently: true });
const rgb = (css) => { canvas.fillStyle = '#000'; canvas.fillStyle = css; canvas.fillRect(0, 0, 1, 1); return [...canvas.getImageData(0, 0, 1, 1).data].slice(0, 3); };
const lum = (c) => c.map((v) => v / 255).map((v) => (v <= 0.04045 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4))
  .reduce((sum, v, i) => sum + v * [0.2126, 0.7152, 0.0722][i], 0);
const ratio = (a, b) => { const [hi, lo] = [lum(a), lum(b)].sort((x, y) => y - x); return Math.floor(((hi + 0.05) / (lo + 0.05)) * 100) / 100; };

const bg = rgb(root.getPropertyValue('--color-bg'));
document.querySelector('#colors').innerHTML = unique.filter((n) => n.startsWith('--color-')).map((n) => `
  <li class="swatch"><span class="chip" style="background: var(${n})"></span>
    <span><code>${n}</code><br><span class="ratio">${ratio(rgb(root.getPropertyValue(n)), bg)}:1</span></span></li>`).join('');
document.querySelector('#spacing').innerHTML = unique.filter((n) => n.startsWith('--space-')).map((n) => `
  <li><code>${n}</code><span class="bar" style="width: var(${n})"></span>${root.getPropertyValue(n)}</li>`).join('');
