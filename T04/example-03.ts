console.log(9007199254740993n, 9007199254740993);
// 9007199254740993n 9007199254740992

function addFee(amount: bigint, fee: number) {
  // Error: Operator '+' cannot be applied to types 'bigint' and 'number'.
  return amount + fee;
}
