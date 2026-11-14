// 08: a 5,000-book catalog for client-side search: 50 classics x 100 editions, deterministic
export interface CatalogEntry {
  isbn: string;
  title: string;
  author: string;
  subjects: string;
}

const classics: [string, string, string][] = [
  ["Dune", "Frank Herbert", "science fiction desert planet politics"],
  ["Emma", "Jane Austen", "romance society comedy england"],
  ["The Hobbit", "J. R. R. Tolkien", "fantasy adventure dragon journey"],
  ["Middlemarch", "George Eliot", "provincial life marriage reform"],
  ["Frankenstein", "Mary Shelley", "gothic science creature horror"],
  ["Pride and Prejudice", "Jane Austen", "romance marriage class england"],
  ["Moby-Dick", "Herman Melville", "whaling sea obsession adventure"],
  ["Jane Eyre", "Charlotte Bronte", "governess romance gothic independence"],
  ["Wuthering Heights", "Emily Bronte", "moors revenge passion gothic"],
  ["Great Expectations", "Charles Dickens", "orphan ambition london convict"],
  ["Bleak House", "Charles Dickens", "court case london mystery"],
  ["Little Women", "Louisa May Alcott", "sisters family growing up"],
  ["Anna Karenina", "Leo Tolstoy", "russia affair society tragedy"],
  ["War and Peace", "Leo Tolstoy", "napoleon russia war families"],
  ["Crime and Punishment", "Fyodor Dostoevsky", "murder guilt saint petersburg"],
  ["The Brothers Karamazov", "Fyodor Dostoevsky", "faith family murder philosophy"],
  ["Madame Bovary", "Gustave Flaubert", "provincial france marriage desire"],
  ["Les Miserables", "Victor Hugo", "paris revolution justice redemption"],
  ["The Count of Monte Cristo", "Alexandre Dumas", "prison revenge treasure"],
  ["Don Quixote", "Miguel de Cervantes", "knight spain satire adventure"],
  ["Ulysses", "James Joyce", "dublin modernism single day"],
  ["Dubliners", "James Joyce", "dublin short stories epiphany"],
  ["Mrs Dalloway", "Virginia Woolf", "london party modernism memory"],
  ["To the Lighthouse", "Virginia Woolf", "family summer time modernism"],
  ["The Great Gatsby", "F. Scott Fitzgerald", "jazz age wealth new york"],
  ["Heart of Darkness", "Joseph Conrad", "congo river colonialism"],
  ["Dracula", "Bram Stoker", "vampire gothic transylvania horror"],
  ["The Picture of Dorian Gray", "Oscar Wilde", "portrait vanity london"],
  ["Treasure Island", "Robert Louis Stevenson", "pirates treasure sea adventure"],
  ["Kidnapped", "Robert Louis Stevenson", "scotland adventure escape"],
  ["The Time Machine", "H. G. Wells", "time travel future science fiction"],
  ["The War of the Worlds", "H. G. Wells", "martians invasion science fiction"],
  ["Around the World in Eighty Days", "Jules Verne", "journey bet adventure"],
  ["Twenty Thousand Leagues Under the Seas", "Jules Verne", "submarine ocean adventure"],
  ["Persuasion", "Jane Austen", "second chance navy romance"],
  ["Sense and Sensibility", "Jane Austen", "sisters romance money england"],
  ["Northanger Abbey", "Jane Austen", "gothic parody romance bath"],
  ["Silas Marner", "George Eliot", "weaver village gold child"],
  ["The Mill on the Floss", "George Eliot", "siblings river village"],
  ["Tess of the d'Urbervilles", "Thomas Hardy", "wessex fate rural tragedy"],
  ["Far from the Madding Crowd", "Thomas Hardy", "wessex farm suitors"],
  ["The Scarlet Letter", "Nathaniel Hawthorne", "puritan boston sin"],
  ["Walden", "Henry David Thoreau", "nature solitude simple living"],
  ["The Adventures of Huckleberry Finn", "Mark Twain", "mississippi river journey"],
  ["The Adventures of Tom Sawyer", "Mark Twain", "boyhood missouri adventure"],
  ["Alice's Adventures in Wonderland", "Lewis Carroll", "fantasy rabbit hole nonsense"],
  ["Gulliver's Travels", "Jonathan Swift", "satire voyages giants"],
  ["Robinson Crusoe", "Daniel Defoe", "shipwreck island survival"],
  ["The Odyssey", "Homer", "epic voyage return ithaca"],
  ["The Call of the Wild", "Jack London", "dog klondike gold survival"],
];

// synthetic ISBN-13 numbers with a valid check digit
function isbn(n: number) {
  const body = `9798${String(n).padStart(8, "0")}`;
  const sum = [...body].reduce((s, d, i) => s + Number(d) * (i % 2 ? 3 : 1), 0);
  return body + ((10 - (sum % 10)) % 10);
}

export const catalog: CatalogEntry[] = Array.from({ length: 5000 }, (_, i) => {
  const [title, author, subjects] = classics[i % classics.length];
  const year = 1926 + Math.floor(i / classics.length);
  return { isbn: isbn(i + 1), title: `${title} (${year} edition)`, author, subjects };
});
