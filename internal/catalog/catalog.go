// Package catalog holds the books Bookstore sells.
package catalog

import (
	"errors"
	"fmt"
	"iter"
	"slices"
)

// Money is an amount in kuruş: 34990 means 349.90 TRY.
type Money int64

// Book is one title in the catalog.
type Book struct {
	ISBN     string `json:"isbn"`
	Title    string `json:"title"`
	Author   string `json:"author"`
	Category string `json:"category"`
	Price    Money  `json:"price"`
	Stock    int    `json:"stock"`
}

// books is unexported: other packages can only reach it through Store.
var books = []Book{
	{ISBN: "9780441172719", Title: "Dune", Author: "Frank Herbert", Category: "Science Fiction", Price: 34990, Stock: 12},
	{ISBN: "9780547928227", Title: "The Hobbit", Author: "J.R.R. Tolkien", Category: "Fantasy", Price: 28950, Stock: 0},
	{ISBN: "9780141439549", Title: "Middlemarch", Author: "George Eliot", Category: "Classics", Price: 31000, Stock: 4},
	{ISBN: "9780141439587", Title: "Emma", Author: "Jane Austen", Category: "Classics", Price: 19900, Stock: 7},
	{ISBN: "9780141439686", Title: "Persuasion", Author: "Jane Austen", Category: "Classics", Price: 18500, Stock: 3},
	{ISBN: "9780141439662", Title: "Sense and Sensibility", Author: "Jane Austen", Category: "Classics", Price: 21000, Stock: 5},
	{ISBN: "9780553293357", Title: "Foundation", Author: "Isaac Asimov", Category: "Science Fiction", Price: 27500, Stock: 9},
	{ISBN: "9780441478125", Title: "The Left Hand of Darkness", Author: "Ursula K. Le Guin", Category: "Science Fiction", Price: 31000, Stock: 2},
}

// ErrBookNotFound is returned when no book has the requested ISBN.
var ErrBookNotFound = errors.New("book not found")

// Store serves books from memory for now.
type Store struct{}

// All returns a copy of every book, so callers cannot change the catalog.
func (Store) All() []Book {
	return slices.Clone(books)
}

// CheaperThan returns the books whose price is at most limit.
func (Store) CheaperThan(limit Money) []Book {
	result := []Book{} // empty, not nil: encodes as [] instead of null
	for _, b := range books {
		if b.Price <= limit {
			result = append(result, b)
		}
	}
	return result
}

// ByISBN returns the book with the given ISBN.
func (Store) ByISBN(isbn string) (Book, error) {
	for _, b := range books {
		if b.ISBN == isbn {
			return b, nil
		}
	}
	return Book{}, fmt.Errorf("isbn %s: %w", isbn, ErrBookNotFound)
}

// Books yields every book in catalog order. Each value is a copy.
func (Store) Books() iter.Seq[Book] {
	return slices.Values(books)
}

// ByAuthor yields each author once, in the order they first appear in the
// catalog, together with that author's books.
func (Store) ByAuthor() iter.Seq2[string, []Book] {
	return func(yield func(string, []Book) bool) {
		var authors []string
		groups := make(map[string][]Book)
		for _, b := range books {
			if _, seen := groups[b.Author]; !seen {
				authors = append(authors, b.Author)
			}
			groups[b.Author] = append(groups[b.Author], b)
		}
		for _, a := range authors {
			if !yield(a, groups[a]) {
				return
			}
		}
	}
}

// String formats the amount for people: Money(34990) prints as "349.90 TRY".
func (m Money) String() string {
	return m.Decimal() + " TRY"
}
