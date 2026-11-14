// Usage: ng build && node scripts/collect-source-maps.mjs
// Moves the hidden source maps out of the folder that gets deployed and lists their debug IDs.
import { mkdirSync, readdirSync, readFileSync, renameSync } from 'node:fs';
import { join } from 'node:path';

const browserDir = 'dist/bookstore-web/browser';
const mapsDir = 'dist/bookstore-web/source-maps';
mkdirSync(mapsDir, { recursive: true });

for (const file of readdirSync(browserDir).filter((name) => name.endsWith('.map'))) {
  const { debugId } = JSON.parse(readFileSync(join(browserDir, file), 'utf8'));
  renameSync(join(browserDir, file), join(mapsDir, file));
  console.log(`${file.padEnd(28)} ${debugId ?? '(no debugId)'}`);
}
