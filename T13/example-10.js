const text = '{"transactionId": 12345678901234567890}';

console.log(JSON.parse(text).transactionId); // 12345678901234567000

const payment = JSON.parse(text, (key, value, context) => {
  if (key === "transactionId") {
    return BigInt(context.source);
  }
  return value;
});
console.log(payment.transactionId); // 12345678901234567890n
