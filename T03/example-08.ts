const prices = [450, 320, 390];

const doubled = prices.map((price) => price * 2);
// Error: Property 'toUpperCase' does not exist on type 'number'.
const labels = prices.map((price) => price.toUpperCase());
