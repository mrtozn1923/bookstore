function checkout() {
  console.log(discount); // ReferenceError: Cannot access 'discount' before initialization
  let discount = 10;
}
checkout();
