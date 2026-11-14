class Customer {
  name: string;
  currency = "TRY";
  // Error: Property 'email' has no initializer and is not definitely assigned in the constructor.
  email: string;

  constructor(name: string) {
    this.name = name;
  }
}
