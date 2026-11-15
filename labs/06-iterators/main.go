// Lab 06: iter.Seq, range-over-func, slices/maps helpers and a broken iterator.
package main

import (
	"fmt"
	"iter"
	"maps"
	"slices"

	"bookstore/internal/catalog"
)

// Filter yields the values of seq for which keep returns true.
func Filter[V any](seq iter.Seq[V], keep func(V) bool) iter.Seq[V] {
	return func(yield func(V) bool) {
		for v := range seq {
			if keep(v) && !yield(v) {
				return
			}
		}
	}
}

// countdown ignores yield's result: a bug the runtime reports.
func countdown(yield func(int) bool) {
	for i := 3; i > 0; i-- {
		yield(i)
	}
}

func main() {
	var store catalog.Store

	// 1) The loop body is the yield function; break makes yield return false.
	for b := range store.Books() {
		if b.Stock == 0 {
			fmt.Println("first sold-out book:", b.Title)
			break
		}
	}

	// 2) A Seq2 yields two values: author and books.
	for author, books := range store.ByAuthor() {
		fmt.Printf("%-15s %d\n", author, len(books))
	}

	// 3) Composing: filter lazily, collect into a slice at the end.
	inStock := Filter(store.Books(), func(b catalog.Book) bool { return b.Stock > 0 })
	cheap := Filter(inStock, func(b catalog.Book) bool { return b.Price < 25000 })
	var titles []string
	for b := range cheap {
		titles = append(titles, b.Title)
	}
	fmt.Println(titles)

	// 4) maps.Keys returns an iterator; map order is random, so sort it.
	stock := map[string]int{}
	for b := range store.Books() {
		stock[b.Title] = b.Stock
	}
	fmt.Println(slices.Collect(maps.Keys(stock))[:3])
	fmt.Println(slices.Sorted(maps.Keys(stock))[:3])

	// 5) An iterator that keeps calling yield after break.
	defer func() { fmt.Println("recovered:", recover()) }()
	for i := range countdown {
		fmt.Println("got", i)
		break
	}
}
