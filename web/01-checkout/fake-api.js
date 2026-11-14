// Stand-in for Bookstore.Api: every call creates an order after 1.2 s.
window.ordersCreated = 0;
window.placeOrder = (order) =>
  new Promise((resolve) => {
    setTimeout(() => {
      window.ordersCreated += 1;
      resolve({ id: `ORD-${1040 + window.ordersCreated}`, ...order });
    }, 1200);
  });
