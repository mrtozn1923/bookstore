abstract class Discount {
  abstract amount(total: number): number;

  describe(total: number): string {
    return `Discount: ${this.amount(total).toFixed(2)} TRY`;
  }
}

class PercentDiscount extends Discount {
  #percent: number;

  constructor(percent: number) {
    super();
    this.#percent = percent;
  }

  amount(total: number): number {
    return (total * this.#percent) / 100;
  }
}

console.log(new PercentDiscount(10).describe(770)); // Discount: 77.00 TRY
// Error: Cannot create an instance of an abstract class.
const discount = new Discount();
