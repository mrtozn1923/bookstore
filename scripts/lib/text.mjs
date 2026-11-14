// Runs in the page: characters per rendered line of the first text in an element.
export function lineStats(sel) {
  const el = document.querySelector(sel);
  const node = el.firstChild;
  const range = document.createRange();
  const lines = new Map();
  for (let i = 0; i < node.length; i++) {
    range.setStart(node, i);
    range.setEnd(node, i + 1);
    const rect = range.getClientRects()[0];
    if (!rect) continue;
    const top = Math.round(rect.top);
    lines.set(top, (lines.get(top) ?? 0) + 1);
  }
  const counts = [...lines.values()];
  const cs = getComputedStyle(el);
  return {
    lines: counts.length,
    avg: Math.round(counts.slice(0, -1).reduce((a, b) => a + b, 0) / Math.max(1, counts.length - 1)),
    max: Math.max(...counts),
    fontSize: cs.fontSize,
    lineHeight: cs.lineHeight,
  };
}

// Runs in the page: x position of the decimal point in each matching cell.
export function decimalPositions(sel) {
  return [...document.querySelectorAll(sel)].map((cell) => {
    const node = cell.firstChild;
    const range = document.createRange();
    const i = node.textContent.lastIndexOf('.');
    range.setStart(node, i);
    range.setEnd(node, i + 1);
    return Math.round(range.getBoundingClientRect().left * 10) / 10;
  });
}
