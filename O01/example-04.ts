function first<T>(items: T[]): T | undefined {
  return items[0];
}

const firstTitle = first(["Dune", "Emma"]); // string | undefined
const firstPrice = first([450, 320]); // number | undefined
