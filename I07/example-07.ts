declare const brand: unique symbol;

type Brand<T, Name extends string> = T & { readonly [brand]: Name };

type CustomerId = Brand<string, "CustomerId">;
type Cents = Brand<number, "Cents">;

function cents(amount: number): Cents {
  if (!Number.isInteger(amount)) {
    throw new Error("Cents must be an integer");
  }
  return amount as Cents;
}

function addCents(a: Cents, b: Cents): Cents {
  return (a + b) as Cents;
}

const total = addCents(cents(45000), cents(32000));
console.log(total); // 77000

const raw = cents(45000) + cents(32000); // raw: number, the brand is gone
