const text = '{"id":1042,"createdAt":"2026-01-15T10:00:00.000Z"}';

const placedOrder = JSON.parse(text, (key, value) => {
  if (key === "createdAt") {
    return new Date(value);
  }
  return value;
});

console.log(placedOrder.createdAt instanceof Date);  // true
console.log(placedOrder.createdAt.getUTCFullYear()); // 2026
