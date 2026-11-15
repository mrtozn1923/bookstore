// Prints the accessibility score and the failed audits of Lighthouse JSON reports.
// Usage: node lh-summary.mjs [--min 100] reports/*.json
// With --min the exit code is 1 when a score is below it: a regression alarm, not proof of accessibility.
import { readFileSync } from 'node:fs';

const args = process.argv.slice(2);
const minAt = args.indexOf('--min');
const min = minAt >= 0 ? Number(args.splice(minAt, 2)[1]) : null;
let below = 0;
for (const file of args) {
  const lhr = JSON.parse(readFileSync(file, 'utf8'));
  const category = lhr.categories.accessibility;
  const failed = category.auditRefs
    .map(ref => lhr.audits[ref.id])
    .filter(a => a.score === 0)
    .map(a => a.id);
  const manual = category.auditRefs.filter(ref => lhr.audits[ref.id].scoreDisplayMode === 'manual').length;
  const score = Math.round(category.score * 100);
  console.log(`${file}: Lighthouse ${lhr.lighthouseVersion} accessibility ${score}`);
  console.log(`  failed: ${failed.join(', ') || 'none'}; manual checks listed: ${manual}`);
  if (min !== null && score < min) below++;
}
if (below) {
  console.log(`${below} report(s) below ${min}`);
  process.exit(1);
}
