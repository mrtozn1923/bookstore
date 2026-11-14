// Which published versions does each range accept? Uses the real version list from the registry
import semver from "semver";

const res = await fetch("https://registry.npmjs.org/express");
const { versions } = await res.json();
const stable = Object.keys(versions).filter((v) => !semver.prerelease(v) && semver.gte(v, "4.21.0"));

for (const range of ["5.2.1", "~5.2.0", "^5.2.0", "^4.21.0", ">=5.0.0 <5.2.0"]) {
    const matches = stable.filter((v) => semver.satisfies(v, range));
    console.log(range.padEnd(16), "→", matches.join(", ") || "(none)");
}

// Caret on 0.x: only the patch may change
console.log("^0.7.1 accepts 0.7.2:", semver.satisfies("0.7.2", "^0.7.1"));
console.log("^0.7.1 accepts 0.8.0:", semver.satisfies("0.8.0", "^0.7.1"));
