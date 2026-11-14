const stockItem = {
  title: "Middlemarch",
  stock: 3,
  sell: function () {
    this.stock = this.stock - 1;
    return this.stock;
  }
};

console.log(stockItem.sell()); // 2
console.log(stockItem.sell()); // 1
console.log(stockItem.stock);  // 1
