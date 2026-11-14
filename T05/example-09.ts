const cartRow: [string, ...number[]] = ["Ada", 450, 320, 390];
const emptyCart: [string, ...number[]] = ["Alan"];
// Error: Type 'number' is not assignable to type 'string'.
const noCustomer: [string, ...number[]] = [450, 320];
