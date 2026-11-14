// Runs after "vite build --mode server": wwwroot is served as static files, and ASP.NET Core
// serves .map files too, so the hidden source maps move out of it before publish
import { mkdirSync, readdirSync, readFileSync, renameSync } from 'node:fs';
import { join } from 'node:path';

const assetsDir = '../server/Bookstore.Web/wwwroot/dist/assets';
const mapsDir = 'source-maps';
mkdirSync(mapsDir, { recursive: true });

for (const file of readdirSync(assetsDir).filter((name) => name.endsWith('.map'))) {
  const { debugId } = JSON.parse(readFileSync(join(assetsDir, file), 'utf8'));
  renameSync(join(assetsDir, file), join(mapsDir, file));
  console.log(`${file.padEnd(28)} ${debugId}`);
}
