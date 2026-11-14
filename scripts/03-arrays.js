// 03 - array queries: $size and $all
printjson(db.books.find({ authors: { $size: 0 } }, { _id: 0, title: 1 }).toArray());
printjson(db.books.find(
    { "authors.name": { $all: ["Neil Gaiman", "Terry Pratchett"] } },
    { _id: 0, title: 1, "authors.name": 1 }
).toArray());
