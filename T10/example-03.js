const titles = ["Dune", "Emma", "The Hobbit"];
console.log(titles[0]);     // "Dune"
console.log(titles.at(-1)); // "The Hobbit"
console.log(titles[5]);     // undefined

titles[1] = "Middlemarch";
console.log(titles);        // ["Dune", "Middlemarch", "The Hobbit"]
