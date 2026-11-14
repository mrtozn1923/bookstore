const broken = "{'items': ['Dune']}";

try {
  const cart = JSON.parse(broken);
  console.log(cart.items);
} catch (error) {
  console.log(error.name);  // "SyntaxError"
  console.log("Couldn't read the saved cart, starting with an empty one.");
}

console.log("The code keeps running.");
