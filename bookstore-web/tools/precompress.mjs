// Runs after `ng build`: writes a Brotli (quality 11) copy next to every text file of the browser build.
// Bookstore.Api serves these instead of compressing on every request. zstd level 19 was larger than
// Brotli 11 for every file of this build (lab/compress-sizes.mjs), so no .zst copies.
import { readdirSync, readFileSync, statSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import zlib from 'node:zlib';

const root = 'dist/bookstore-web/browser';
const { BROTLI_PARAM_QUALITY, BROTLI_PARAM_MODE, BROTLI_MODE_TEXT } = zlib.constants;
const files = readdirSync(root, { recursive: true }).map(String).filter((f) => /\.(js|css|html|txt|svg|json)$/.test(f));
let raw = 0;
let br = 0;
for (const file of files) {
  const path = join(root, file);
  const input = readFileSync(path);
  const brotli = zlib.brotliCompressSync(input, { params: { [BROTLI_PARAM_QUALITY]: 11, [BROTLI_PARAM_MODE]: BROTLI_MODE_TEXT } });
  // A compressed copy that is not smaller is useless (tiny files)
  if (brotli.length < input.length) writeFileSync(path + '.br', brotli);
  raw += statSync(path).size;
  br += Math.min(brotli.length, input.length);
}
console.log(`precompressed ${files.length} files: ${(raw / 1000).toFixed(1)} kB -> br ${(br / 1000).toFixed(1)} kB`);
