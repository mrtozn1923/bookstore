const batch = "BKS-2026-000123\nBKS-2026-000124\nBKS-2026-000125\n";

console.log(batch.match(/^BKS-\d+-\d+$/g));
console.log(batch.match(/^BKS-\d+-\d+$/gm).length);

// JS has no \A, \z or \Z; $ without the m flag means the very end
console.log(/^BKS-\d+-\d+$/.test("BKS-2026-000123\n"));

// \b uses ASCII \w, so a boundary appears between "p" and "ç"
console.log(/\bkitap\b/.test("kitapçı"), /\bkitap\b/u.test("kitapçı"));
