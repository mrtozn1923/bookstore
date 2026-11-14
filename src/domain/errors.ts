// Domain errors know nothing about HTTP; the error middleware maps them to status codes
export class DomainError extends Error {}

export class BookNotFoundError extends DomainError {
    constructor(isbn: string) {
        super(`book ${isbn} was not found`);
        this.name = "BookNotFoundError";
    }
}

export class OrderNotFoundError extends DomainError {
    constructor(id: string) {
        super(`order ${id} was not found`);
        this.name = "OrderNotFoundError";
    }
}

export class UnknownBookError extends DomainError {
    readonly isbns: readonly string[];

    constructor(isbns: readonly string[]) {
        super(`unknown ISBN: ${isbns.join(", ")}`);
        this.name = "UnknownBookError";
        this.isbns = isbns;
    }
}

export class MixedCurrencyError extends DomainError {
    constructor() {
        super("all books in one order must have the same currency");
        this.name = "MixedCurrencyError";
    }
}
