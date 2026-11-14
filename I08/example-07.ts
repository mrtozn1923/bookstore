function oldLog(target: object, key: string, descriptor: PropertyDescriptor) {
  const original = descriptor.value;
  descriptor.value = function (this: unknown, ...args: unknown[]) {
    console.log(`call ${key}`);
    return original.apply(this, args);
  };
}

class StockService {
  @oldLog
  reserve() {
    return "reserved";
  }
}

console.log(new StockService().reserve());
