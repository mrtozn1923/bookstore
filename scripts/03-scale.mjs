// Modular type scale: base * ratio^step, for steps -1..4
const base = 16;
for (const [name, ratio] of [['minor third', 1.2], ['major third', 1.25], ['perfect fourth', 1.333]]) {
  const steps = [-1, 0, 1, 2, 3, 4].map((s) => base * ratio ** s);
  console.log(`${name.padEnd(15)} ${ratio}: ${steps.map((v) => v.toFixed(1)).join(' / ')}  ->  ${steps.map((v) => Math.round(v)).join(' / ')}`);
}
