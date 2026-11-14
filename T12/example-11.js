const withMethod = { name: "Ada", greet: function () { return "Hello"; } };

try {
  structuredClone(withMethod);
} catch (error) {
  console.log(error.name); // "DataCloneError"
}
