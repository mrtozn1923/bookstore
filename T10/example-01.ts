type Logger = (message: string) => void;

const log: Logger = (message) => message.length;

const prices = [450, 320, 390];
const copy: number[] = [];
prices.forEach((price) => copy.push(price));

function resetCart(): void {
  return 0;
}
