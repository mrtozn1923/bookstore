function greet(name = "guest") {
  console.log(`Hello ${name}`);
}

greet("Ada"); // Hello Ada
greet();       // Hello guest
