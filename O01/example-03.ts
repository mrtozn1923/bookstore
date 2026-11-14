function firstItem(items: any[]): any {
  return items[0];
}

const title = firstItem(["Dune", "Emma"]);
title.toFixed(2); // Compiles; throws a TypeError when it runs
