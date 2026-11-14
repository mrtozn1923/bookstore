type SearchOptions = { query: string; pageSize?: number };

const options = { query: "Dune", pagesize: 50 };
const applied: SearchOptions = options; // compiles: query exists, pageSize is optional
console.log(applied.pageSize); // undefined
