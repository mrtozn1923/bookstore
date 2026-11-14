import path from "node:path";

console.log(path.join("uploads", "covers", "9780441172719.jpg"));
console.log(path.join("uploads", "/etc/passwd"));
console.log(path.resolve("uploads", "/etc/passwd"));
console.log(path.join("uploads/covers", "../../src/server.ts"));
console.log(path.win32.join("C:\\bookstore", "uploads", "covers"));
console.log(path.extname("cover.final.JPG"), path.basename("/data/catalog.csv", ".csv"));
console.log(import.meta.dirname === path.dirname(import.meta.filename));
