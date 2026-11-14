// Catalogue data shared by both pages: 30 titles, each in one to three formats (70 items).
const titles = [
  ['Dune', 'Frank Herbert', 'science-fiction', 1965],
  ['Foundation', 'Isaac Asimov', 'science-fiction', 1951],
  ['The Left Hand of Darkness', 'Ursula K. Le Guin', 'science-fiction', 1969],
  ['Solaris', 'Stanislaw Lem', 'science-fiction', 1961],
  ['The Time Machine', 'H. G. Wells', 'science-fiction', 1895],
  ['The War of the Worlds', 'H. G. Wells', 'science-fiction', 1898],
  ['Fahrenheit 451', 'Ray Bradbury', 'science-fiction', 1953],
  ['The Hobbit', 'J. R. R. Tolkien', 'fantasy', 1937],
  ['A Wizard of Earthsea', 'Ursula K. Le Guin', 'fantasy', 1968],
  ['The Last Unicorn', 'Peter S. Beagle', 'fantasy', 1968],
  ["Alice's Adventures in Wonderland", 'Lewis Carroll', 'fantasy', 1865],
  ['The Wonderful Wizard of Oz', 'L. Frank Baum', 'fantasy', 1900],
  ['Frankenstein', 'Mary Shelley', 'gothic', 1818],
  ['Dracula', 'Bram Stoker', 'gothic', 1897],
  ['Wuthering Heights', 'Emily Brontë', 'gothic', 1847],
  ['The Picture of Dorian Gray', 'Oscar Wilde', 'gothic', 1890],
  ['Rebecca', 'Daphne du Maurier', 'gothic', 1938],
  ['Emma', 'Jane Austen', 'classics', 1815],
  ['Persuasion', 'Jane Austen', 'classics', 1817],
  ['Pride and Prejudice', 'Jane Austen', 'classics', 1813],
  ['Sense and Sensibility', 'Jane Austen', 'classics', 1811],
  ['Middlemarch', 'George Eliot', 'classics', 1871],
  ['Jane Eyre', 'Charlotte Brontë', 'classics', 1847],
  ['Great Expectations', 'Charles Dickens', 'classics', 1861],
  ['Bleak House', 'Charles Dickens', 'classics', 1853],
  ['Moby-Dick', 'Herman Melville', 'classics', 1851],
  ['Anna Karenina', 'Leo Tolstoy', 'classics', 1878],
  ['Madame Bovary', 'Gustave Flaubert', 'classics', 1857],
  ['Crime and Punishment', 'Fyodor Dostoevsky', 'classics', 1866],
  ['The Count of Monte Cristo', 'Alexandre Dumas', 'classics', 1844],
];
const formats = ['paperback', 'hardcover', 'ebook'];

window.categories = [
  { slug: 'classics', name: 'Classics' },
  { slug: 'science-fiction', name: 'Science fiction' },
  { slug: 'fantasy', name: 'Fantasy' },
  { slug: 'gothic', name: 'Gothic' },
];
window.formatNames = { paperback: 'Paperback', hardcover: 'Hardcover', ebook: 'E-book' };

window.books = titles.flatMap(([title, author, category, year], i) =>
  formats.slice(0, 1 + (i % 3) + (i % 2)).slice(0, 3).map((format, f) => ({
    id: `${title.toLowerCase().replace(/[^a-z0-9]+/g, '-')}-${format}`,
    title, author, category, year, format,
    price: Math.round((150 + ((i * 37 + f * 53) % 300)) * 100) / 100 + (format === 'ebook' ? -60 : format === 'hardcover' ? 120 : 0),
  })),
);
