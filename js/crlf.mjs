import { readFileSync } from "node:fs";

// same text in JavaScript: \r is a line terminator for ^, $ and .
const text = readFileSync("data/app.log", "utf8").replace(/\n/g, "\r\n");

console.log(text.match(/BKS-\d{4}-\d{6}$/gm).length);
console.log(text.match(/declined.+$/m)[0].replace("\r", "\\r"));
