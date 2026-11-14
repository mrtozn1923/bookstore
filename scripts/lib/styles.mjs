// Runs in the page: describes every visible element that has its own text.
export function collectTextStyles() {
  const rgb = (s) => s.match(/[\d.]+/g).slice(0, 4).map(Number);
  const background = (el) => {
    for (let e = el; e; e = e.parentElement) {
      const c = rgb(getComputedStyle(e).backgroundColor);
      if (c.length < 4 || c[3] > 0) return c.slice(0, 3);
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
      color: rgb(cs.color).slice(0, 3),
      bg: background(el),
      top: Math.round(box.top + scrollY),
      area: Math.round(box.width * box.height),
    });
  }
  return out;
}
