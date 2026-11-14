var message = "first";
var message = "second";
console.log(message);

{
  var leak = "I escaped the block";
}
console.log(leak);
