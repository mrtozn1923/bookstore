// Turns the raw axe results of the test run (reports/axe/*.json) into a report per WCAG 2.2 Level A criterion,
// the level of the Ministry's checklist. "automated: no issues" is not "conforms": axe only checks part of each criterion.
// Usage: node report.mjs   (after npx playwright test; writes reports/a11y-report.md)
import { readdirSync, readFileSync, writeFileSync } from 'node:fs';

// The 31 Level A success criteria of WCAG 2.2 (4.1.1 Parsing is obsolete and not listed)
const levelA = ['1.1.1', '1.2.1', '1.2.2', '1.2.3', '1.3.1', '1.3.2', '1.3.3', '1.4.1', '1.4.2', '2.1.1', '2.1.2', '2.1.4',
  '2.2.1', '2.2.2', '2.3.1', '2.4.1', '2.4.2', '2.4.3', '2.4.4', '2.5.1', '2.5.2', '2.5.3', '2.5.4', '3.1.1', '3.2.1',
  '3.2.2', '3.2.6', '3.3.1', '3.3.2', '3.3.7', '4.1.2'];

// axe tags criteria as "wcag111", "wcag1412": turn them back into "1.1.1", "1.4.12"
const criteria = tags => tags.filter(t => /^wcag\d{3,4}$/.test(t)).map(t => `${t[4]}.${t[5]}.${t.slice(6)}`);

const byCriterion = new Map(levelA.map(sc => [sc, { failed: new Set(), passed: new Set(), review: new Set() }]));
const files = readdirSync('reports/axe').filter(f => f.endsWith('.json'));
for (const file of files) {
  const results = JSON.parse(readFileSync(`reports/axe/${file}`, 'utf8'));
  for (const [list, key] of [[results.violations, 'failed'], [results.passes, 'passed'], [results.incomplete, 'review']]) {
    for (const rule of list) {
      for (const sc of criteria(rule.tags)) {
        byCriterion.get(sc)?.[key].add(rule.id);
      }
    }
  }
}

const rows = [...byCriterion].map(([sc, r]) => {
  const status = r.failed.size ? `FAILED (${[...r.failed].join(', ')})`
    : r.review.size ? `needs review (${[...r.review].join(', ')})`
    : r.passed.size ? `automated: no issues (${[...r.passed].join(', ')})`
    : 'manual only';
  return `| ${sc} | ${status} |`;
});
const count = word => rows.filter(row => row.includes(word)).length;
const summary = `${files.length} page states, ${levelA.length} Level A criteria: ${count('FAILED')} failed, ` +
  `${count('needs review')} need review, ${count('automated: no issues')} with automated checks and no issues, ` +
  `${count('manual only')} manual only`;
writeFileSync('reports/a11y-report.md', [
  '# Bookstore accessibility report (WCAG 2.2 Level A)', '', summary, '',
  '| Criterion | Automated result |', '| --- | --- |', ...rows, '',
  'Every criterion still needs the manual checklist and a screen reader pass.', '',
].join('\n'));
console.log(summary);
console.log(rows.filter(row => !row.includes('manual only')).join('\n'));
