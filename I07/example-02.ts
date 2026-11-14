interface PriceInTry {
  amount: number;
}
interface PriceInEur {
  amount: number;
}

function showEuroPrice(price: PriceInEur) {
  console.log(`${price.amount} EUR`);
}

const dunePrice: PriceInTry = { amount: 450 };
showEuroPrice(dunePrice);
