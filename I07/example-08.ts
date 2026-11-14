class Kilograms {
  #kind = "kilograms";
  value: number;
  constructor(value: number) {
    this.value = value;
  }
}

class Pounds {
  #kind = "pounds";
  value: number;
  constructor(value: number) {
    this.value = value;
  }
}

function shipParcel(weight: Kilograms) {
  console.log(`Shipping a ${weight.value} kg parcel`);
}

shipParcel(new Pounds(3)); // Error
shipParcel({ value: 3 }); // Error
