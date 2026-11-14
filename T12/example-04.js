const listing = { "book title": "Emma", price: 320 };
console.log(listing["book title"]); // "Emma"

const field = "price";
console.log(listing[field]);        // 320
console.log(listing.field);         // undefined
