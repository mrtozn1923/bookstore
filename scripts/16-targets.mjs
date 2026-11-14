import { createRequire } from 'node:module';
import { readdir, readFile } from 'node:fs/promises';
import path from 'node:path';

// the browserslist copy the Angular build itself uses (installed with angular/)
const browserslist = createRequire(path.resolve('angular/package.json'))('browserslist');

// oldest version of each browser a query lets in
const oldest = query => {
    const min = {};
    for (const entry of browserslist(query)) {
        const [name, version] = entry.split(' ');
        const v = parseFloat(version);
        if (['chrome', 'edge', 'firefox', 'safari', 'ios_saf'].includes(name) && !(min[name] <= v)) min[name] = v;
    }
    return Object.entries(min).map(([name, v]) => `${name} ${v}`).join(', ');
};

const queries = [
    ['Angular 22.2 default', 'baseline widely available on 2026-05-07'],
    ['Vite 8 default', 'baseline widely available on 2026-01-01'],
    ['widely available today', 'baseline widely available on 2026-11-14'],
    ['newly available', 'baseline newly available'],
];
for (const [label, query] of queries) console.log(`${label.padEnd(24)} ${oldest(query)}`);

// what the last `ng build` did with the nested header rule in src/styles.css
const dist = path.resolve('angular/dist/bookstore-web/browser');
const css = (await readdir(dist)).find(f => f.startsWith('styles-') && f.endsWith('.css'));
const built = await readFile(path.join(dist, css), 'utf8');
// the header rule up to its matching closing brace, plus a flattened "header a" rule if the build made one
const rule = start => {
    let depth = 0;
    for (let i = built.indexOf('{', start); i < built.length; i++) {
        depth += built[i] === '{' ? 1 : built[i] === '}' ? -1 : 0;
        if (depth === 0) return built.slice(start, i + 1);
    }
};
const rules = [built.indexOf('app-root>header{'), built.indexOf('app-root>header a{')].filter(i => i >= 0).map(rule);
console.log(`${css}: ${rules.join(' ')}`);
