interface Base {
  id: string;
}

// Error: Interface 'Item' incorrectly extends interface 'Base'.
interface Item extends Base {
  id: number;
}

type BaseShape = { id: string };
type ItemShape = BaseShape & { id: number };

// Error: Type 'number' is not assignable to type 'never'.
const item: ItemShape = { id: 1 };
