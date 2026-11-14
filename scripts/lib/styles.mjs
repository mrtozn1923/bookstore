// Runs in the page: describes every visible element that has its own text.
// Colours are resolved through a canvas, so oklch() and other syntaxes end up as sRGB bytes.
export function collectTextStyles() {
  const ctx = document.createElement('canvas').getContext('2d', { willReadFrequently: true });
  const rgba = (color) => {
    ctx.clearRect(0, 0, 1, 1);
    ctx.fillStyle = color;
    ctx.fillRect(0, 0, 1, 1);
    return [...ctx.getImageData(0, 0, 1, 1).data];
  };
  const background = (el) => {
    for (let e = el; e; e = e.parentElement) {
      const c = rgba(getComputedStyle(e).backgroundColor);
      if (c[3] > 0) return c.slice(0, 3);
    }
    return [255, 255, 255];
  };
  const out = [];
  for (const el of document.querySelectorAll('body *')) {
    const own = [...el.childNodes].some((n) => n.nodeType === 3 && n.textContent.trim());
    const box = el.getBoundingClientRect();
    if (!own || box.width === 0) continue;
    const cs = getComputedStyle(el);
    out.push({
      key: el.dataset.key ?? '',
      text: el.textContent.trim().slice(0, 24),
      size: parseFloat(cs.fontSize),
      weight: Number(cs.fontWeight),
      color: rgba(cs.color).slice(0, 3),
      bg: background(el),
      border: rgba(cs.borderTopColor).slice(0, 3),
      parentBg: background(el.parentElement),
      top: Math.round(box.top + scrollY),
      area: Math.round(box.width * box.height),
    });
  }
  return out;
}
