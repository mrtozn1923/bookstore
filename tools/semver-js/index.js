import semver from "semver";

console.log("RC.1 vs rc.1 (npm):", semver.compare("1.0.0-RC.1", "1.0.0-rc.1"));
console.log("valid 2026.04.1:", semver.valid("2026.04.1"));
console.log("valid v1.2.3:", semver.valid("v1.2.3"));

const checks = [
  ["1.9.0", "^1.2.3"],
  ["2.0.0", "^1.2.3"],
  ["0.2.9", "^0.2.3"],
  ["0.3.0", "^0.2.3"],
  ["0.0.4", "^0.0.3"],
  ["1.3.0-beta.1", "^1.2.0"],
  ["1.2.4-beta.1", "^1.2.4-alpha.1"],
];
for (const [version, range] of checks) {
  console.log(`${version.padEnd(13)} ${range.padEnd(15)} ${semver.satisfies(version, range)}`);
}
