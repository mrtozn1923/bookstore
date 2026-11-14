import { readFileSync } from "node:fs";

function isIsbn13(input) {
  // format: 978/979 prefix, four hyphens, 17 characters in total
  if (!/^97[89]-[0-9]{1,5}-[0-9]{1,7}-[0-9]{1,7}-[0-9]$/.test(input) || input.length !== 17) return false;
  const digits = input.replaceAll("-", "");
  let sum = 0;
  for (let i = 0; i < 12; i++) sum += Number(digits[i]) * (i % 2 === 0 ? 1 : 3);
  return (10 - (sum % 10)) % 10 === Number(digits[12]);
}

console.log(isIsbn13("978-0-441-17271-9"), isIsbn13("978-0-441-17271-8"), isIsbn13("978-0-441-17271-9\n"));

const mail = readFileSync("data/support-mail.txt", "utf8");
for (const m of mail.matchAll(/\b97[89]-[0-9]{1,5}-[0-9]{1,7}-[0-9]{1,7}-[0-9]\b/g)) {
  console.log(m[0], "at", m.index, isIsbn13(m[0]));
}
