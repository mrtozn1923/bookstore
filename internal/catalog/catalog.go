// Package catalog holds the books Bookstore sells.
package catalog

import (
	"errors"
	"fmt"
	"slices"
)

// Money is an amount in kuruş: 34990 means 349.90 TRY.
type Money int64

// Book is one title in the catalog.
type Book struct {
	ISBN   string `json:"isbn"`
	Title  string `json:"title"`
	Author string `json:"author"`
	Price  Money  `json:"price"`
	Stock  int    `json:"stock"`
}

// books is unexported: other packages can only reach it through Store.
var books = []Book{
	{ISBN: "9780441172719", Title: "Dune", Author: "Frank Herbert", Price: 34990, Stock: 12},
	{ISBN: "9780547928227", Title: "The Hobbit", Author: "J.R.R. Tolkien", Price: 28950, Stock: 0},
	{ISBN: "9780141439549", Title: "Middlemarch", Author: "George Eliot", Price: 31000, Stock: 4},
	{ISBN: "9780141439587", Title: "Emma", Author: "Jane Austen", Price: 19900, Stock: 7},
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

// String formats the amount for people: Money(34990) prints as "349.90 TRY".
func (m Money) String() string {
	return fmt.Sprintf("%d.%02d TRY", m/100, m%100)
}
