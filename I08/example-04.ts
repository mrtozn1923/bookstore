const registry = new Map<string, Function>();

function register(name: string) {
  return function (target: Function, context: ClassDecoratorContext) {
    console.log(`kind: ${context.kind}, name: ${String(context.name)}`);
    registry.set(name, target);
  };
}

@register("csv")
class OrderCsvExporter {
  export(rows: string[][]) {
    return rows.map((row) => row.join(",")).join("\n");
  }
}

console.log([...registry.keys()]); // [ 'csv' ]
