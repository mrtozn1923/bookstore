class Customer {
  name = "Ada";
  contact() {
    return "ada@example.com";
  }
}

class Distributor {
  name = "Book Distributor";
  contact() {
    return "orders@example.com";
  }
}

const customer: Customer = new Distributor(); // compiles: the shapes match
console.log(customer.contact()); // orders@example.com
