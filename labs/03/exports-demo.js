// Self-reference: a package can import itself by name through its "exports"
import { formatPrice } from "bookstore-pricing";
console.log(formatPrice(310));

try {
    await import("bookstore-pricing/cart.cjs");
} catch (err) {
    console.log(err.code);
    console.log(err.message.split(" imported from")[0]);
}
