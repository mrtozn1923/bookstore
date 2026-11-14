type Customer = {
  id: number;
  name: string;
  email: string;
  passwordHash: string;
};

type CustomerPreview = Pick<Customer, "id" | "name">;
// { id: number; name: string; }

type PublicCustomer = Omit<Customer, "passwordHash">;
// { id: number; name: string; email: string; }

function toPublic(customer: Customer): PublicCustomer {
  const { passwordHash, ...rest } = customer;
  return rest;
}

// Error: Type '"avatar"' does not satisfy the constraint 'keyof Customer'.
type WithAvatar = Pick<Customer, "avatar">;
