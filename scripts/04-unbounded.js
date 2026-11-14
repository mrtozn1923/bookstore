// 04 - unbounded array: embedding every review of a book in the book
db.lab_books.drop();
const template = db.reviews.findOne({ body: { $exists: true } }, { _id: 0, bookId: 0 });
const book = db.books.findOne({ _id: 26 });
db.lab_books.insertOne(book);
print("book alone           :", bsonsize(db.lab_books.findOne({ _id: 26 })), "bytes");

const batch = Array.from({ length: 10000 }, () => template);
let pushed = 0;
try {
    while (true) {
        db.lab_books.updateOne({ _id: 26 }, { $push: { reviews: { $each: batch } } });
        pushed += batch.length;
        if (pushed % 20000 === 0) {
            print(`${String(pushed).padStart(6)} reviews  :`, bsonsize(db.lab_books.findOne({ _id: 26 })), "bytes");
        }
    }
} catch (e) {
    print(`after ${pushed} reviews: ${e.name} code ${e.code}`);
    print(e.message);
}
db.lab_books.drop();
