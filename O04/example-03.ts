class Logger {
  protected prefix = "[bookstore]";

  public info(message: string): void {
    console.log(`${this.prefix} ${message}`);
  }
}

class RequestLogger extends Logger {
  request(path: string): void {
    this.info(`GET ${path}`);
    console.log(this.prefix.length);
  }
}

const logger = new RequestLogger();
logger.request("/books");
// Error: Property 'prefix' is protected and only accessible within class 'Logger' and its subclasses.
console.log(logger.prefix);
