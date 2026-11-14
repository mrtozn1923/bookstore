const order = {
  id: 1042,
  customer: {
    name: "Grace",
    address: { city: "Istanbul", zip: "34000" }
  },
  items: [
    { title: "Dune", price: 450 },
    { title: "Emma", price: 320 }
  ]
};

console.log(order.customer.name);         // "Grace"
console.log(order.customer.address.city); // "Istanbul"
console.log(order.items[1].title);        // "Emma"
console.log(order.items.length);          // 2
