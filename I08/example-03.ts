function bound(target: Function, context: ClassMethodDecoratorContext) {
  const name = context.name;
  context.addInitializer(function (this: any) {
    // we don't track the type of the bind result here, hence any
    this[name] = this[name].bind(this);
  });
}

class Customer {
  name: string;
  constructor(name: string) {
    this.name = name;
  }

  @bound
  greet() {
    console.log(`Welcome back, ${this.name}`);
  }
}

const { greet } = new Customer("Ada");
greet(); // Welcome back, Ada
