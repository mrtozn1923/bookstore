type MyAwaited<T> = T extends Promise<infer V> ? MyAwaited<V> : T;

type N = MyAwaited<Promise<Promise<number>>>; // number
type S = MyAwaited<string>; // string

async function loadCustomer() {
  return { id: 1, name: "Ada" };
}

type Customer = MyAwaited<ReturnType<typeof loadCustomer>>; // { id: number; name: string; }

function greet(customer: Customer) {
  console.log(`Welcome back, ${customer.name}`);
}

loadCustomer().then(greet); // Welcome back, Ada
