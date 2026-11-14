// Shapes and rules shared by the API and other Bookstore Node.js packages
export const ORDER_STATUSES = ["Pending", "Paid", "Shipped", "Delivered", "Cancelled"];

export function isValidIsbn13(isbn) {
    if (!/^97[89]\d{10}$/.test(isbn)) {
        return false;
    }
    const sum = [...isbn].reduce((acc, ch, i) => acc + Number(ch) * (i % 2 === 0 ? 1 : 3), 0);
    return sum % 10 === 0;
}

export function toBookSummary(book) {
    return {
        isbn: book.isbn,
        title: book.title,
        price: { amount: book.price, currency: book.currency },
    };
}
