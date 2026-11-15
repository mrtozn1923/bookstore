export type CartLine = {
  isbn: string;
  title: string;
  unitPrice: number;
  quantity: number;
  isEbook: boolean;
};

export const sampleCart = (): CartLine[] => [
  { isbn: "9780441013593", title: "Dune", unitPrice: 189.5, quantity: 1, isEbook: false },
  { isbn: "9780141439587", title: "Emma", unitPrice: 59.9, quantity: 1, isEbook: true },
  { isbn: "9780261103344", title: "The Hobbit", unitPrice: 149.9, quantity: 0, isEbook: false },
];
