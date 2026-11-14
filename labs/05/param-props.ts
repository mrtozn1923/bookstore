class BookService {
    constructor(private readonly currency: string) {}

    format(amount: number): string {
        return `${amount.toFixed(2)} ${this.currency}`;
    }
}

console.log(new BookService("TRY").format(349.9));
