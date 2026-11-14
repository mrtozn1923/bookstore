// The order total as the API computed it up to post 11: prices as JavaScript numbers
const items = [
    { isbn: "9780441172719", title: "Dune", quantity: 3, unitPrice: 349.9 },
    { isbn: "9780547928227", title: "The Hobbit", quantity: 1, unitPrice: 289.5 },
];

const total = items.reduce((sum, item) => sum + item.quantity * item.unitPrice, 0);
console.log(total);
console.log(JSON.stringify({ total }));
console.log(total === 1339.2);
