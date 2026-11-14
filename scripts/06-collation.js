// 06 - collation: case-insensitive Turkish search and sort with an index
db.books.dropIndexes();
const tr = { locale: "tr", strength: 2 };
db.books.createIndex({ title: 1 }, { collation: tr });

function summary(label, explain) {
    const s = explain.executionStats;
    const stages = [];
    for (let st = s.executionStages; st; st = st.inputStage) stages.push(st.stage);
    print(`${label.padEnd(18)} ${stages.join(" <- ").padEnd(16)} keys ${String(s.totalKeysExamined).padStart(2)}  docs ${String(s.totalDocsExamined).padStart(2)}  returned ${s.nReturned}`);
}

// The query must use the same collation as the index
summary("with collation", db.books.find({ title: "ince memed" }).collation(tr).explain("executionStats"));
summary("without collation", db.books.find({ title: "ince memed" }).explain("executionStats"));

// Sort: binary order versus the Turkish alphabet
const titles = c => db.books.find({ title: { $gte: "H" } }, { _id: 0, title: 1 }).sort({ title: 1 }).collation(c).limit(5).toArray().map(b => b.title);
print("simple:", db.books.find({}, { _id: 0, title: 1 }).sort({ title: -1 }).limit(3).toArray().map(b => b.title).reverse().join(" | "));
print("tr    :", titles(tr).join(" | "));

// Locale matters: in Turkish, I and i are different letters (I/ı, İ/i)
for (const c of [tr, { locale: "en", strength: 2 }]) {
    print(`${c.locale}: city "istanbul" ->`, db.customers.countDocuments({ city: "istanbul" }, { collation: c }),
          ` title "ince memed" ->`, db.books.countDocuments({ title: "ince memed" }, { collation: c }));
}
db.books.dropIndexes();
