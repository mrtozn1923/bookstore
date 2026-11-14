function handleClick(this: { label: string }) {
  console.log(`Clicked: ${this.label}`);
}

const button = { label: "Add to cart", handleClick };
button.handleClick(); // Clicked: Add to cart

const detached = button.handleClick;
// Error: The 'this' context of type 'void' is not assignable to method's 'this' of type '{ label: string; }'.
detached();
