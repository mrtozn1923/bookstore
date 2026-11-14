function trimmed(target: undefined, context: ClassFieldDecoratorContext<unknown, string>) {
  return function (initial: string) {
    return initial.trim();
  };
}

function clamp(min: number, max: number) {
  return function (
    target: ClassAccessorDecoratorTarget<unknown, number>,
    context: ClassAccessorDecoratorContext<unknown, number>
  ): ClassAccessorDecoratorResult<unknown, number> {
    return {
      set(value) {
        target.set.call(this, Math.min(max, Math.max(min, value)));
      },
      init(value) {
        return Math.min(max, Math.max(min, value));
      },
    };
  };
}

class CartLine {
  @trimmed title = "  Dune  ";
  @clamp(1, 10) accessor quantity = 15;
}

const line = new CartLine();
console.log(line.title); // Dune
console.log(line.quantity); // 10
line.quantity = -2;
console.log(line.quantity); // 1
