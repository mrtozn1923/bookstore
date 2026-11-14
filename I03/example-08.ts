interface Customer {
  id: number;
  name: string;
  passwordHash: string;
  save(): void;
}

type PublicCustomer = { [K in keyof Customer as Exclude<K, "passwordHash">]: Customer[K] };
// { id: number; name: string; save: () => void; }
