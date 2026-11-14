// the pattern of an Angular Validators.pattern(...) on the customer name field
const naive = /^([a-zA-Z]+\s?)*$/;
const safe = /^[a-zA-Z]+(?: [a-zA-Z]+)*$/;

for (const name of ["Ada Lovelace", "Grace Hopper-Smith", "Maximiliane Josephine Vanderbilt-Rockefeller"]) {
  for (const [label, re] of [["naive", naive], ["safe", safe]]) {
    const start = performance.now();
    const ok = re.test(name);
    console.log(`${label.padEnd(5)} ${String(ok).padEnd(5)} ${(performance.now() - start).toFixed(1).padStart(8)} ms  ${name}`);
  }
}
