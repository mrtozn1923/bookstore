function tag(label: string) {
  console.log(`evaluate ${label}`);
  return function (target: Function, context: ClassMethodDecoratorContext) {
    console.log(`apply ${label} to ${String(context.name)}`);
  };
}

class Invoice {
  @tag("first")
  @tag("second")
  print() {
    console.log("printing");
  }
}

console.log("class ready");
