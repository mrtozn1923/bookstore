function confirmShipping(name, city) {
  console.log(`${name}, your order ships to ${city}.`);
}

confirmShipping("Ada", "Izmir");            // Ada, your order ships to Izmir.
confirmShipping("Grace");                   // Grace, your order ships to undefined.
confirmShipping("Alan", "Ankara", "extra"); // Alan, your order ships to Ankara.
