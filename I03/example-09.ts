type Discount = { kind: "percent"; rate: number } | { kind: "fixed"; amount: number };

type DiscountHandlers = { [D in Discount as D["kind"]]: (discount: D, price: number) => number };

const applyDiscount: DiscountHandlers = {
  percent: (discount, price) => price - (price * discount.rate) / 100,
  fixed: (discount, price) => price - discount.amount,
};

console.log(applyDiscount.percent({ kind: "percent", rate: 10 }, 450)); // 405
