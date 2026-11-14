// Usage: ng build --stats-json && node scripts/bundle-report.mjs
// Groups the bytes of the main bundle by npm package (or "src" for our own code).
import { readFileSync } from 'node:fs';

const stats = JSON.parse(readFileSync('dist/bookstore-web/browser-stats.json', 'utf8'));

function packageOf(input) {
  const index = input.lastIndexOf('node_modules/');
  if (index < 0) {
    return 'src';
  }
  const parts = input.slice(index + 'node_modules/'.length).split('/');
  return parts[0].startsWith('@') ? `${parts[0]}/${parts[1]}` : parts[0];
}

for (const [file, output] of Object.entries(stats.outputs)) {
  if (output.entryPoint !== 'src/main.ts') {
    continue;
  }
  const byPackage = new Map();
  for (const [input, { bytesInOutput }] of Object.entries(output.inputs)) {
    byPackage.set(packageOf(input), (byPackage.get(packageOf(input)) ?? 0) + bytesInOutput);
  }
  const total = [...byPackage.values()].reduce((sum, bytes) => sum + bytes, 0);
  console.log(`${file}: ${(output.bytes / 1000).toFixed(2)} kB`);
  for (const [name, bytes] of [...byPackage].sort((a, b) => b[1] - a[1]).slice(0, 8)) {
    const share = ((bytes / total) * 100).toFixed(1).padStart(5);
    console.log(`  ${name.padEnd(28)} ${share}%`);
  }
}
