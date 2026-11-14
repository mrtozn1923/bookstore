type Customer = {
  id: number;
  name: string;
  address: { city: string; zip: string };
};

type A = Customer["address"]["city"];
type B = Customer["id" | "name"];
type C = Customer["phone"];
