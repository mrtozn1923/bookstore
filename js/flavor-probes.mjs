import { readFileSync } from "node:fs";

// the same probe file in JavaScript: without flags and with the u flag
const lines = readFileSync("data/flavor-probes.tsv", "utf8").trim().split("\n");
const cell = (pattern, flags, input) => {
  try {
    const m = new RegExp(pattern, flags).exec(input);
    return m ? m[0] : "-";
  } catch (e) {
    return "error: " + e.message.split(": ").at(-1);
  }
};
console.log("probe".padEnd(14) + "no flag".padEnd(28) + "u flag");
for (const line of lines) {
  const [name, pattern, input] = line.split("\t");
  console.log(name.padEnd(14) + cell(pattern, "", input).padEnd(28) + cell(pattern, "u", input));
}
