// Prints the accessibility score and the failed audits of Lighthouse JSON reports.
// Usage: node lh-summary.mjs reports/*.json
import { readFileSync } from 'node:fs';

for (const file of process.argv.slice(2)) {
  const lhr = JSON.parse(readFileSync(file, 'utf8'));
  const category = lhr.categories.accessibility;
  const failed = category.auditRefs
    .map(ref => lhr.audits[ref.id])
    .filter(a => a.score === 0)
    .map(a => a.id);
  const manual = category.auditRefs.filter(ref => lhr.audits[ref.id].scoreDisplayMode === 'manual').length;
  console.log(`${file}: Lighthouse ${lhr.lighthouseVersion} accessibility ${Math.round(category.score * 100)}`);
  console.log(`  failed: ${failed.join(', ') || 'none'}; manual checks listed: ${manual}`);
}
