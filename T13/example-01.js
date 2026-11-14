const order = {
  id: 1042,
  note: undefined,
  getTotal: function () { return 770; },
  createdAt: new Date("2026-01-15T10:00:00Z"),
  discount: NaN
};

console.log(JSON.stringify(order));
