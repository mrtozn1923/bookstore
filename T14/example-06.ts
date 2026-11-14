const stock = document.querySelector("#stock");
const received = document.querySelector("#received");

if (stock instanceof HTMLInputElement && received instanceof HTMLInputElement) {
  const wrong = stock.value + received.value; // string: "12" + "5" → "125"
  const right = stock.valueAsNumber + received.valueAsNumber; // number: 17
  console.log(wrong, right);
}
