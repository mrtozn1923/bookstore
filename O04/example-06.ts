class Order {
  readonly id: number;
  readonly createdAt = new Date();

  constructor(id: number) {
    this.id = id;
  }
}

const order = new Order(1042);
// Error: Cannot assign to 'id' because it is a read-only property.
order.id = 1043;
