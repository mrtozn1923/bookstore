// Anti-pattern: swallow every crash and keep going
const reservations: { isbn: string; quantity: number }[] = [];
let reservedUnits = 0;

process.on("uncaughtException", (err) => {
    console.log(`ignored: ${err.message}`);
});

function reserveStock(isbn: string, quantity: number, warehouse: string | undefined): void {
    reservedUnits += quantity;
    // A bug between the two steps of the update
    const code = warehouse!.toUpperCase();
    reservations.push({ isbn, quantity });
    console.log(`reserved ${quantity} x ${isbn} in ${code}`);
}

setTimeout(() => reserveStock("9780441172719", 3, undefined), 0);
setTimeout(() => console.log(`reservedUnits: ${reservedUnits}, reservations: ${reservations.length}`), 10);
