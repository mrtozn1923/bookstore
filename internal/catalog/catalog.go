// Package catalog holds the books Bookstore sells.
package catalog

// Book is one title in the catalog.
type Book struct {
	ISBN  string `json:"isbn"`
	Title string `json:"title"`
}

// books is unexported: other packages can only reach it through Store.
var books = []Book{
	{ISBN: "9780441172719", Title: "Dune"},
	{ISBN: "9780547928227", Title: "The Hobbit"},
	{ISBN: "9780141439549", Title: "Middlemarch"},
}

// Store serves books from memory for now.
type Store struct{}

// All returns every book in the catalog.
func (Store) All() []Book {
	return books
}
