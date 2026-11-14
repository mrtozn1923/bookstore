function isStringArray(value: unknown): value is string[] {
  return Array.isArray(value) && value.every((item) => typeof item === "string");
}

const saved: unknown = JSON.parse('["Dune", "Emma"]');

if (isStringArray(saved)) {
  console.log(saved.join(", ")); // Dune, Emma
}
