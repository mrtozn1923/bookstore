export interface Book {
    isbn: string;
    title: string;
    author: string;
    price: number;
}

export const sampleBooks: Book[] = [
    { isbn: "9780441172719", title: "Dune", author: "Frank Herbert", price: 450 },
    { isbn: "9780141439587", title: "Emma", author: "Jane Austen", price: 210 },
    { isbn: "9780547928227", title: "The Hobbit", author: "J. R. R. Tolkien", price: 320 },
    { isbn: "9780141439549", title: "Middlemarch", author: "George Eliot", price: 280 },
    { isbn: "9780141439518", title: "Pride and Prejudice", author: "Jane Austen", price: 195 },
];
