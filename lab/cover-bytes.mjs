// Prints the size of one cover per width and format as Bookstore.Api serves it (format chosen by Accept).
// Usage: node cover-bytes.mjs [name]
const name = process.argv[2] ?? 'dune';
const base = 'http://127.0.0.1:5300';
const accepts = {
  jpeg: 'image/jpeg,*/*',
  webp: 'image/webp,image/jpeg,*/*',
  avif: 'image/avif,image/webp,image/jpeg,*/*',
};
const kb = (n) => `${(n / 1024).toFixed(1)} KB`.padStart(10);
const original = await fetch(`${base}/covers/${name}.jpg`).then((r) => r.arrayBuffer());
console.log(`original /covers/${name}.jpg (1200x1800): ${kb(original.byteLength).trim()}`);
console.log('width        jpeg      webp      avif');
for (const w of [320, 480, 960, 1200]) {
  const sizes = [];
  for (const accept of Object.values(accepts)) {
    const res = await fetch(`${base}/img/covers/${name}?w=${w}`, { headers: { Accept: accept } });
    sizes.push(kb((await res.arrayBuffer()).byteLength));
  }
  console.log(`${String(w).padEnd(5)}${sizes.join('')}`);
}
const res = await fetch(`${base}/img/covers/${name}?w=480`, { headers: { Accept: accepts.avif } });
console.log(`Content-Type: ${res.headers.get('content-type')}, Vary: ${res.headers.get('vary')}, Cache-Control: ${res.headers.get('cache-control')}`);
