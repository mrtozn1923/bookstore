// 05 - bucket pattern: book views in hourly documents
db.book_views.drop();
const start = ISODate("2026-06-30T09:00:00Z").getTime();
for (let i = 0; i < 1000; i++) {
    const at = new Date(start + i * 11 * 1000);              // one view every 11 seconds
    const hour = new Date(Math.floor(at.getTime() / 3600000) * 3600000);
    db.book_views.updateOne(
        { bookId: 26, hour },
        { $inc: { count: 1, [`byCountry.${["TR", "GB", "DE"][i % 3]}`]: 1 } },
        { upsert: true }
    );
}
print("documents:", db.book_views.countDocuments());
db.book_views.find().sort({ hour: 1 }).forEach(b =>
    print(b.hour.toISOString(), "count", b.count, "TR", b.byCountry.TR, "GB", b.byCountry.GB, "DE", b.byCountry.DE));
