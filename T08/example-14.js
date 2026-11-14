let language = "javascript";
language = language[0].toUpperCase() + language.slice(1);
console.log(language); // "Javascript"

const loud = language.toUpperCase();
console.log(loud);     // "JAVASCRIPT"
console.log(language); // "Javascript"
