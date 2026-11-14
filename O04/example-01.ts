class Payment {
  private amount = 770;
  #cardToken = "tok_1042";
}

const payment = new Payment();
console.log(payment.amount);
console.log(payment["amount"]);
console.log(Object.keys(payment));
