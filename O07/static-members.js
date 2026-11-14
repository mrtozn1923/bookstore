class Customer {
  static count = 0;

  constructor(name) {
    this.name = name;
    Customer.count++;
  }

  static fromJSON(json) {
    const data = JSON.parse(json);
    return new Customer(data.name);
  }
}

const linus = Customer.fromJSON('{"name":"Linus"}');
console.log(linus.name);     // "Linus"
console.log(Customer.count); // 1
console.log(linus.fromJSON); // undefined
