// Compiles the same file 30 times with the pure JS package and with sass-embedded.
import * as sass from "sass";
import * as embedded from "sass-embedded";

const file = "01/bench.scss";
const runs = 30;

function measure(label, compileFn) {
    compileFn(file); // warm-up
    const start = performance.now();
    for (let i = 0; i < runs; i++) compileFn(file);
    const avg = (performance.now() - start) / runs;
    console.log(`${label.padEnd(26)} ${avg.toFixed(0).padStart(5)} ms per compile`);
}

measure("sass (pure JS)", (f) => sass.compile(f));
const compiler = embedded.initCompiler();
measure("sass-embedded (Dart VM)", (f) => compiler.compile(f));
compiler.dispose();
