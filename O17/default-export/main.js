import Cart from "./Cart.js";
import ShoppingCart from "./Cart.js"; // same class, different name

const cart = new Cart();
cart.add({ isbn: "9780441172719", title: "Dune" });
console.log(cart.size); // 1
