const original = {
  id: 1042,
  createdAt: new Date("2026-01-15T10:00:00Z"),
  giftNote: undefined,
  shippingAddress: { city: "Izmir" }
};

const viaJson = JSON.parse(JSON.stringify(original));
const viaClone = structuredClone(original);

console.log(typeof viaJson.createdAt);           // "string"
console.log(viaClone.createdAt instanceof Date); // true
console.log("giftNote" in viaJson);              // false
console.log("giftNote" in viaClone);             // true
