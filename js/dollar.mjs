const banner = "Dune: {discount}";
const discount = "$& off";   // text typed by a store admin

console.log(banner.replace(/\{discount\}/, discount));
console.log(banner.replace(/\{discount\}/, discount.replaceAll("$", "$$$$")));
console.log(banner.replace(/\{discount\}/, () => discount));
console.log(banner.replace(/\{discount\}/, "$0.99 off"));
