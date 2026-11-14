const level = "2026-11-15T10:42:12Z err Bookstore.Payments Payment 4713 declined";

const cases = [
  ["(?i:ERR) Bookstore\\.Payments", ""],
  ["ERR (?-i:Bookstore)\\.payments", "i"],
  ["ERR (?-i:bookstore)\\.payments", "i"],
  ["(?i)ERR Bookstore\\.Payments", ""],
  ["(?x:ERR)", ""],
  ["(?i-i:ERR)", ""],
];

for (const [pattern, flags] of cases) {
  try {
    console.log(`/${pattern}/${flags}`.padEnd(36), new RegExp(pattern, flags).test(level));
  } catch (e) {
    console.log(`/${pattern}/${flags}`.padEnd(36), `${e.name}: ${e.message}`);
  }
}
