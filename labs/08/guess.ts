// A Turkish classic: how long is the title?
const title = "Çalıkuşu";
const bytes = Buffer.from(title, "utf8");

console.log(title.length);
console.log(bytes.length);
console.log(bytes.subarray(0, 1).toString("utf8"));
