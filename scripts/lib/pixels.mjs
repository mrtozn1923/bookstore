// Compares two PNG screenshots of the same region inside the page (canvas decoding, no extra packages).
// Returns how many pixels changed at all and how many changed with at least a 3:1 contrast.
export async function comparePng(page, a, b) {
  return page.evaluate(async ([a, b]) => {
    const decode = async (b64) => {
      const bitmap = await createImageBitmap(await (await fetch(`data:image/png;base64,${b64}`)).blob());
      const canvas = new OffscreenCanvas(bitmap.width, bitmap.height);
      const ctx = canvas.getContext('2d');
      ctx.drawImage(bitmap, 0, 0);
      return ctx.getImageData(0, 0, bitmap.width, bitmap.height).data;
    };
    const channel = (c) => {
      const v = c / 255;
      return v <= 0.04045 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4;
    };
    const lum = (r, g, b) => 0.2126 * channel(r) + 0.7152 * channel(g) + 0.0722 * channel(b);
    const [p, q] = await Promise.all([decode(a), decode(b)]);
    let changed = 0;
    let strong = 0;
    for (let i = 0; i < p.length; i += 4) {
      if (p[i] === q[i] && p[i + 1] === q[i + 1] && p[i + 2] === q[i + 2]) continue;
      changed++;
      const [hi, lo] = [lum(p[i], p[i + 1], p[i + 2]), lum(q[i], q[i + 1], q[i + 2])].sort((x, y) => y - x);
      if ((hi + 0.05) / (lo + 0.05) >= 3) strong++;
    }
    return { changed, strong };
  }, [a.toString('base64'), b.toString('base64')]);
}
