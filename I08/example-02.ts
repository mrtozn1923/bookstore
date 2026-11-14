function logged<This, Args extends unknown[], Return>(
  target: (this: This, ...args: Args) => Return,
  context: ClassMethodDecoratorContext<This, (this: This, ...args: Args) => Return>
) {
  const name = String(context.name);
  return function (this: This, ...args: Args): Return {
    console.log(`-> ${name}(${args.join(", ")})`);
    const result = target.call(this, ...args);
    console.log(`<- ${name} = ${result}`);
    return result;
  };
}

class Cart {
  @logged
  lineTotal(price: number, quantity: number) {
    return price * quantity;
  }
}

new Cart().lineTotal(450, 2);
// -> lineTotal(450, 2)
// <- lineTotal = 900
