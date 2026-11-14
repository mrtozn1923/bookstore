namespace Shipping {
  export const FREE_SHIPPING_OVER = 500;

  export function fee(total: number): number {
    return total >= FREE_SHIPPING_OVER ? 0 : 60;
  }
}

console.log(Shipping.fee(450)); // 60
