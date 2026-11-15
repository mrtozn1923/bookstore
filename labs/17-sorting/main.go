// Lab 17: sorting and searching with slices and cmp.
package main

import (
	"cmp"
	"fmt"
	"slices"

	"bookstore/internal/catalog"
)

func main() {
	books := catalog.Store{}.All()

	// Category ascending, then price descending, then title.
	slices.SortFunc(books, func(a, b catalog.Book) int {
		return cmp.Or(
			cmp.Compare(a.Category, b.Category),
			cmp.Compare(b.Price, a.Price), // b before a: descending
			cmp.Compare(a.Title, b.Title),
		)
	})
	for _, b := range books {
		fmt.Printf("%-16s %-26s %v\n", b.Category, b.Title, b.Price)
	}

	isbns := make([]string, 0, len(books))
	for _, b := range books {
		isbns = append(isbns, b.ISBN)
	}
	slices.Sort(isbns) // BinarySearch needs a sorted slice
	i, found := slices.BinarySearch(isbns, "9780441172719")
	j, missing := slices.BinarySearch(isbns, "9780000000002")
	fmt.Println(i, found, j, missing)

	cheapest := slices.MinFunc(books, func(a, b catalog.Book) int { return cmp.Compare(a.Price, b.Price) })
	k := slices.IndexFunc(books, func(b catalog.Book) bool { return b.Stock == 0 })
	fmt.Println(cheapest.Title, k, books[k].Title)
}
