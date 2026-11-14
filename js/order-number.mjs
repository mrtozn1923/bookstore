const inputs = ["BKS-2026-000123", "BKS-2026-000123\n", "BKS-2026-٠٠٠١٢٣"];
const patterns = [/^BKS-\d{4}-\d{6}$/, /^BKS-\d{4}-\d{6}$/u];

for (const pattern of patterns) {
  console.log(String(pattern));
  for (const input of inputs) {
    console.log(`  ${JSON.stringify(input).padEnd(26)} ${pattern.test(input)}`);
  }
}
