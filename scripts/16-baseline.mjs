import { features } from 'web-features';

// the features this series used, from :has() to anchor positioning, plus the ones still on the horizon
const ids = [
    'cascade-layers', 'has', 'nesting', 'container-queries', 'subgrid', 'media-query-range-syntax', 'color-mix', 'oklab',
    'linear-easing', 'registered-custom-properties', 'starting-style', 'transition-behavior', 'light-dark', 'relative-color',
    'popover', 'view-transitions', 'scrollbar-color', 'invoker-commands', 'anchor-positioning', 'shape-function', 'scope',
    'contrast-color', 'container-name-queries', 'container-style-queries', 'field-sizing', 'sibling-count',
    'progress-function', 'alpha', 'scroll-driven-animations',
    'if', 'function', 'random-function', 'customizable-select', 'cross-document-view-transitions', 'display-animation',
    'grid-lanes', 'interest-invokers',
];

const browsers = support => Object.entries(support)
    .filter(([browser]) => ['chrome', 'firefox', 'safari'].includes(browser))
    .map(([browser, version]) => `${browser} ${version}`).join(', ') || 'no browser data';

const rows = ids.map(id => ({ id, ...features[id] }));
const baseline = rows.filter(f => f.status.baseline).sort((a, b) => a.status.baseline_low_date.localeCompare(b.status.baseline_low_date));
const limited = rows.filter(f => !f.status.baseline);

for (const f of baseline) {
    const level = f.status.baseline === 'high' ? `widely ${f.status.baseline_high_date}` : 'newly';
    console.log(`${f.status.baseline_low_date}  ${f.id.padEnd(30)} ${level}`);
}
console.log('--- not Baseline (November 2026)');
for (const f of limited) {
    // how many of the feature's compat keys are Baseline on their own
    const keys = Object.values(f.status.by_compat_key ?? {});
    const ready = keys.filter(k => k.baseline).length;
    console.log(`${f.id.padEnd(32)} ${browsers(f.status.support).padEnd(38)} keys Baseline ${ready}/${keys.length}`);
}
