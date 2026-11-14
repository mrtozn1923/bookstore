import { parse } from "node:url";

// Legacy redirect helper from an old Bookstore admin script
const target = parse("https://shop.example.com/books/9780441172719?ref=mail");
console.log(target.pathname);

// WHATWG URL: the replacement that throws no deprecation warning
console.log(new URL("https://shop.example.com/books/9780441172719?ref=mail").pathname);
