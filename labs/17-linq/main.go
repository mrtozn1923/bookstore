// Lab 17: what LINQ's GroupBy, Sum and Distinct look like in Go.
package main

import (
	"fmt"
	"iter"
	"maps"
	"slices"

	"bookstore/internal/catalog"
)

// GroupBy collects the values of seq under their key, like ToLookup in C#.
func GroupBy[K comparable, V any](seq iter.Seq[V], key func(V) K) map[K][]V {
	groups := make(map[K][]V)
	for v := range seq {
		k := key(v)
		groups[k] = append(groups[k], v)
	}
	return groups
}

func main() {
	store := catalog.Store{}

	// books.GroupBy(b => b.Author).Where(g => g.Count() > 1)
	byAuthor := GroupBy(store.Books(), func(b catalog.Book) string { return b.Author })
	for _, author := range slices.Sorted(maps.Keys(byAuthor)) {
		if n := len(byAuthor[author]); n > 1 {
			fmt.Println(author, n)
		}
	}

	// books.Where(b => b.Stock > 0).Sum(b => b.Price * b.Stock)
	var value catalog.Money
	for b := range store.Books() {
		if b.Stock > 0 {
			value += b.Price * catalog.Money(b.Stock)
		}
	}
	fmt.Println("stock value:", value)

	// books.Select(b => b.Category).Distinct()
	seen := make(map[string]struct{}) // a set: struct{} takes no memory
	var categories []string
	for b := range store.Books() {
		if _, ok := seen[b.Category]; !ok {
			seen[b.Category] = struct{}{}
			categories = append(categories, b.Category)
		}
	}
	fmt.Println(categories)
}
