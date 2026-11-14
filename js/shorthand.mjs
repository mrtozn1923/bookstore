// in JavaScript \d and \w are ASCII-only, even with the u flag
console.log(/^\d$/.test("٣"), /^\d$/u.test("٣"), /^\p{Nd}$/u.test("٣"));
console.log(/^\w+$/.test("Şükrü"), /^\w+$/u.test("Şükrü"), /^\p{L}+$/u.test("Şükrü"));
console.log(/^\s$/.test(" "));

console.log("A_z^`[".match(/[A-z]/g).join(""));

console.log(/@example.com$/.test("orders@exampleXcom"), /@example\.com$/.test("orders@exampleXcom"));
console.log(/Dune.Emma/.test("Dune\nEmma"), /Dune.Emma/s.test("Dune\nEmma"));
