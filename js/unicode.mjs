// \w stays ASCII in JavaScript; \p{L} needs the u or v flag
console.log(/^\w+$/u.test("Çalıkuşu"), /^\p{L}+$/u.test("Çalıkuşu"), /^\p{Lu}\p{Ll}+$/u.test("Çalıkuşu"));

// without u, \p{L} is just the letters "p{L}"
console.log(/\p{L}/.test("p{L}"), /\p{L}/.test("Dune"));

// v flag: set subtraction and intersection inside a class
const nonAsciiLetter = /[\p{L}--[a-zA-Z]]/v;
console.log(nonAsciiLetter.test("Dune"), nonAsciiLetter.test("Çalıkuşu"));
console.log(/^[\p{Script=Cyrillic}&&\p{Lu}]/v.test("Война и мир"));

// properties of strings exist only with v
console.log(/^\p{RGI_Emoji}$/v.test("👍🏽"), /^\p{Emoji}$/u.test("👍🏽"));
