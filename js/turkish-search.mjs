const catalog = ["ÇALIKUŞU", "İNCE MEMED", "ROBINSON CRUSOE: ISSIZ BİR ADADA 28 YIL"];
const terms = ["çalıkuşu", "ince memed", "ıssız"];

for (const term of terms) {
  const re = new RegExp(RegExp.escape(term), "iu");
  // lower-case both sides with Turkish rules, then search without the i flag
  const trRe = new RegExp(RegExp.escape(term.toLocaleLowerCase("tr-TR")), "u");
  const regexHits = catalog.filter((t) => re.test(t)).length;
  const trHits = catalog.filter((t) => trRe.test(t.toLocaleLowerCase("tr-TR"))).length;
  console.log(`${term.padEnd(11)} iu: ${regexHits}  tr-TR lower: ${trHits}`);
}
