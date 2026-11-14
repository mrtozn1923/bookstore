const book = { title: "Dune", price: 450 };
// { title: string; price: number; }

const prices = [450, 320, 390];
// number[]

const details = ["Dune", 1965];
// (string | number)[]

function getTotal(price: number, quantity: number) {
  return price * quantity;
}

const total = getTotal(450, 2);
// number
