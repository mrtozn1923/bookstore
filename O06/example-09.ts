type OrdersResponse = {
  data: {
    orders: {
      id: number;
      total: number;
      items: { isbn: string; quantity: number }[];
    }[];
  };
  meta: { page: number; pageSize: number };
};

type Order = OrdersResponse["data"]["orders"][number];
type OrderItem = Order["items"][number];
type Pagination = OrdersResponse["meta"];

function countItems(order: Order): number {
  return order.items.reduce((sum: number, item: OrderItem) => sum + item.quantity, 0);
}

function nextPage(meta: Pagination): number {
  return meta.page + 1;
}
