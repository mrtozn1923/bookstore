// T06: run with `node T06/examples.js` (or paste the blocks into the browser console in order)

// --- example 1 ---
const status = "Paid";
switch (status) {
  case "Pending":
    console.log("Waiting for payment");
  case "Paid":
    console.log("Preparing your order");
  case "Shipped":
    console.log("Your order is on the way");
  default:
    console.log("Unknown status");
}

// --- example 2 ---
const stock = 3;
if (stock < 5) {
  console.log("Only a few copies left, order soon.");
}
console.log("Stock checked.");

// --- example 3 ---
const rating = 4.2;
if (rating >= 4.5) {
  console.log("Excellent");
} else if (rating >= 3.5) {
  console.log("Good");
} else if (rating >= 2.5) {
  console.log("Average");
} else {
  console.log("Poor");
}

// --- example 4 ---
const couponCode = "";
if (couponCode) {
  console.log("Applying coupon " + couponCode);
} else {
  console.log("No coupon entered.");
}

// --- example 5 ---
const discount = 0;
if (discount) {
  console.log("Discount: " + discount + "%");
} else {
  console.log("No discount info.");
}

// --- example 6 ---
const frankensteinStock = 0;
const stockLabel = frankensteinStock > 0 ? "In stock" : "Out of stock";
console.log(stockLabel);

// --- example 7 ---
const pageCount = 320;
const bookLength = pageCount > 600 ? "long" : pageCount > 250 ? "medium" : "short";
console.log(bookLength);

// --- example 8 ---
const category = "classics";
switch (category) {
  case "classics":
    console.log("Classics: 10% off this week");
    break;
  case "fantasy":
    console.log("Fantasy: free bookmark with every order");
    break;
  default:
    console.log("No campaign for this category.");
}

// --- example 9 ---
const orderStatus = "Shipped";
switch (orderStatus) {
  case "Shipped":
  case "Delivered":
    console.log("This order can no longer be cancelled.");
    break;
  default:
    console.log("This order can still be cancelled.");
}

// --- example 10 ---
function calculateDiscount(amount, isMember) {
  if (amount <= 0) {
    return 0;
  }
  if (!isMember) {
    return amount;
  }
  return amount * 0.9;
}
console.log(calculateDiscount(450, true));
console.log(calculateDiscount(450, false));
console.log(calculateDiscount(-5, true));

// --- example 11 ---
const choice = "2";
switch (choice) {
  case 1:
    console.log("one");
    break;
  case 2:
    console.log("two");
  case "2":
    console.log("text two");
  default:
    console.log("default");
}
