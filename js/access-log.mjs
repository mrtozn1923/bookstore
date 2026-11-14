import { readFileSync } from "node:fs";

const line = /^\S+ \S+ \S+ \[[^\]]*\] "([A-Z]+) ([^ "]+) [^"]*" ([0-9]{3}) [0-9-]+ "[^"]*" "[^"]*" ([0-9]+\.[0-9]+)$/;

for (const text of readFileSync("data/access.log", "utf8").split(/\r?\n/).filter(Boolean)) {
  const m = line.exec(text);
  if (!m) {
    console.log(`no match: ${text}`);
    continue;
  }
  const [, method, path, status, duration] = m;
  const flag = Number(duration) >= 1 ? "  <-- slow" : "";
  console.log(`${status} ${method.padEnd(4)} ${path.padEnd(36)} ${duration}s${flag}`);
}
