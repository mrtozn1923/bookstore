// Lab 17: the older sort package next to the generic slices package.
package sortbench

import (
	"cmp"
	"math/rand/v2"
	"slices"
	"sort"
	"testing"

	"bookstore/internal/catalog"
)

// shuffled returns 10,000 books with random prices; the seed is fixed.
func shuffled() []catalog.Book {
	r := rand.New(rand.NewPCG(1, 2))
	books := make([]catalog.Book, 10_000)
	for i := range books {
		books[i] = catalog.Book{Title: "Dune", Price: catalog.Money(r.IntN(100_000))}
	}
	return books
}

func BenchmarkSortSlice(b *testing.B) {
	src := shuffled()
	books := make([]catalog.Book, len(src))
	for b.Loop() {
		copy(books, src)
		sort.Slice(books, func(i, j int) bool { return books[i].Price < books[j].Price })
	}
}

func BenchmarkSlicesSortFunc(b *testing.B) {
	src := shuffled()
	books := make([]catalog.Book, len(src))
	for b.Loop() {
		copy(books, src)
		slices.SortFunc(books, func(x, y catalog.Book) int { return cmp.Compare(x.Price, y.Price) })
	}
}
