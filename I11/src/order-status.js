// @ts-check
/** @enum {string} */
const OrderStatus = {
  Pending: "Pending",
  Shipped: "Shipped",
};

/** @param {OrderStatus} status */
function show(status) {
  console.log(status);
}
show("Pending");
