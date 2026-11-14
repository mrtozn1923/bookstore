// Shared sample data for the JavaScript series (Bookstore, the site's shared example app).
// Prices are in TRY; ISBN-13 values have valid check digits.
export const books = [
  { isbn: "9780441172719", title: "Dune", author: "Frank Herbert", price: 450, stock: 12, category: "science-fiction" },
  { isbn: "9780141439587", title: "Emma", author: "Jane Austen", price: 320, stock: 8, category: "classics" },
  { isbn: "9780547928227", title: "The Hobbit", author: "J. R. R. Tolkien", price: 390, stock: 5, category: "fantasy" },
  { isbn: "9780141439549", title: "Middlemarch", author: "George Eliot", price: 380, stock: 3, category: "classics" },
  { isbn: "9780141439471", title: "Frankenstein", author: "Mary Shelley", price: 280, stock: 0, category: "classics" },
  { isbn: "9780141439518", title: "Pride and Prejudice", author: "Jane Austen", price: 300, stock: 7, category: "classics" }
];

export const customers = [
  { id: 1, name: "Ada", email: "ada@example.com", city: "Izmir" },
  { id: 2, name: "Alan", email: "alan@example.com", city: "Ankara" },
  { id: 3, name: "Grace", email: "grace@example.com", city: "Istanbul" },
  { id: 4, name: "Linus", email: "linus@example.com", city: "Ankara" }
];

// Order status values used across the series.
export const OrderStatus = Object.freeze({
  Pending: "Pending",
  Paid: "Paid",
  Shipped: "Shipped",
  Delivered: "Delivered",
  Cancelled: "Cancelled"
});

export const currency = "TRY";
