// OKLCH -> sRGB (Björn Ottosson's OKLab matrices, as used by CSS Color 4).
function oklchToLinear(L, C, H) {
  const h = (H * Math.PI) / 180;
  const a = C * Math.cos(h);
  const b = C * Math.sin(h);
  const l = (L + 0.3963377774 * a + 0.2158037573 * b) ** 3;
  const m = (L - 0.1055613458 * a - 0.0638541728 * b) ** 3;
  const s = (L - 0.0894841775 * a - 1.291485548 * b) ** 3;
  return [
    4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
    -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
    -0.0041960863 * l - 0.7034186147 * m + 1.707614701 * s,
  ];
}

const inGamut = (rgb) => rgb.every((v) => v >= -0.0001 && v <= 1.0001);
const encode = (v) => (v <= 0.0031308 ? 12.92 * v : 1.055 * v ** (1 / 2.4) - 0.055);

// Lowers chroma until the colour fits in sRGB, keeping lightness and hue.
export function toSrgb(L, C, H) {
  let c = C;
  while (!inGamut(oklchToLinear(L, c, H)) && c > 0) c = Math.max(0, c - 0.001);
  const rgb = oklchToLinear(L, c, H).map((v) => Math.round(Math.min(1, Math.max(0, encode(v))) * 255));
  return { rgb, chroma: Math.round(c * 1000) / 1000 };
}
