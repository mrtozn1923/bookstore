import { PrismaPg } from "@prisma/adapter-pg";
import { Prisma, PrismaClient } from "./generated/prisma/client.ts";

// Prisma 7: the generated client talks to PostgreSQL through a driver adapter (here pg)
const prisma = new PrismaClient({
    adapter: new PrismaPg({ connectionString: process.env.DATABASE_URL }),
    log: [{ emit: "event", level: "query" }],
});
let queries = 0;
prisma.$on("query", (e) => {
    queries++;
    if (process.argv.includes("--sql")) {
        console.log(`  SQL: ${e.query}`);
    }
});

const books = await prisma.book.findMany({
    where: { category: { slug: "classics" } },
    select: { isbn: true, title: true, price: true, currency: true, author: { select: { name: true } } },
    orderBy: { title: "asc" },
    take: 10,
});
console.log(books.map((b) => `${b.title} | ${b.author.name} | ${b.price.toFixed(2)}`).join("\n"));
// numeric(10,2) becomes Prisma.Decimal: exact, unlike number
console.log(`price is Prisma.Decimal: ${books[0].price instanceof Prisma.Decimal}, queries: ${queries}`);

// The N+1 trap: the author loaded in a loop, one query per book
queries = 0;
const plain = await prisma.book.findMany({ where: { category: { slug: "classics" } }, orderBy: { title: "asc" } });
for (const book of plain) {
    const author = await prisma.author.findUnique({ where: { id: book.authorId } });
    void author;
}
console.log(`N+1 version: ${plain.length} books, queries: ${queries}`);
await prisma.$disconnect();
