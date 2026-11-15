// Posts SYNTHETIC beacons to /rum: made-up values to exercise the report, not real visitors.
const base = process.argv[2];
const ua = {
  phone: 'Mozilla/5.0 (Linux; Android 15) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/142.0.0.0 Mobile Safari/537.36',
  desktop: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/142.0.0.0 Safari/537.36',
};
let seed = 42;
const random = () => (seed = (seed * 16807) % 2147483647) / 2147483647;
// median value times a skewed factor: most visits fast, a long slow tail
const sample = median => median * Math.exp((random() - 0.5) * 1.6);

const plan = [
  { group: '/blog/:slug', device: 'phone', visits: 80, lcp: 2300, inp: 160, cls: 0.04 },
  { group: '/blog/:slug', device: 'desktop', visits: 80, lcp: 1200, inp: 60, cls: 0.02 },
  { group: '/', device: 'phone', visits: 20, lcp: 3100, inp: 120, cls: 0.01 },
];
let n = 0;
for (const p of plan) {
  for (let i = 0; i < p.visits; i++) {
    const visit = ++n;
    const beacons = [
      { id: `sim-${visit}-lcp`, name: 'LCP', value: Math.round(sample(p.lcp)), pageGroup: p.group },
      { id: `sim-${visit}-cls`, name: 'CLS', value: +sample(p.cls).toFixed(3), pageGroup: p.group },
    ];
    if (i % 2 === 0) beacons.push({ id: `sim-${visit}-inp`, name: 'INP', value: Math.round(sample(p.inp)), pageGroup: p.group });
    await fetch(base + '/rum', { method: 'POST', headers: { 'content-type': 'application/json', 'user-agent': ua[p.device] }, body: JSON.stringify(beacons) });
  }
}
console.log(`posted ${n} synthetic visits`);
