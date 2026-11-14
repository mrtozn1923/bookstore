const tax = 18;

function calcPrice() {
  console.log(tax); // ReferenceError, not 18
  const tax = 20;
}
calcPrice();
