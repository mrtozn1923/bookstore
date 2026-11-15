//go:build go1.27

// Lab 06: Go 1.27 generic methods. The build constraint above raises this
// file's language version to 1.27; the module itself stays at go 1.26.0.
package main

import (
	"fmt"
	"math/rand/v2"

	"bookstore/internal/catalog"
)

// List is a slice with chainable helpers.
type List[T any] []T

// Map converts every element; U is the method's own type parameter.
func (l List[T]) Map[U any](f func(T) U) List[U] {
	out := make(List[U], 0, len(l))
	for _, v := range l {
		out = append(out, f(v))
	}
	return out
}

// Where keeps the elements for which keep returns true.
func (l List[T]) Where(keep func(T) bool) List[T] {
	var out List[T]
	for _, v := range l {
		if keep(v) {
			out = append(out, v)
		}
	}
	return out
}

func main() {
	books := List[catalog.Book](catalog.Store{}.All())
	titles := books.
		Where(func(b catalog.Book) bool { return b.Author == "Jane Austen" }).
		Map(func(b catalog.Book) string { return b.Title })
	fmt.Println(titles, len(titles))

	prices := books.Map(func(b catalog.Book) catalog.Money { return b.Price })
	fmt.Println(prices[:2])

	r := rand.New(rand.NewPCG(1, 2))
	fmt.Println(r.N(10), r.N(catalog.Money(100))) // math/rand/v2: (*Rand).N is generic in 1.27
}
