// Lab 06: type parameters, constraints and the generic paging.Page.
package main

import (
	"cmp"
	"fmt"

	"bookstore/internal/catalog"
	"bookstore/internal/paging"
)

// Integer is any type whose underlying type is int or int64,
// so named types such as catalog.Money qualify too.
type Integer interface {
	~int | ~int64
}

// Sum adds up the values.
func Sum[N Integer](values []N) N {
	var total N
	for _, v := range values {
		total += v
	}
	return total
}

// MaxBy returns the item with the largest key; ok is false for an empty slice.
func MaxBy[T any, K cmp.Ordered](items []T, key func(T) K) (best T, ok bool) {
	for i, it := range items {
		if i == 0 || key(it) > key(best) {
			best, ok = it, true
		}
	}
	return best, ok
}

// IndexBy builds a lookup map; K must be comparable to be a map key.
func IndexBy[K comparable, V any](items []V, key func(V) K) map[K]V {
	m := make(map[K]V, len(items))
	for _, it := range items {
		m[key(it)] = it
	}
	return m
}

func main() {
	books := catalog.Store{}.All()

	prices := make([]catalog.Money, 0, len(books))
	stock := make([]int, 0, len(books))
	for _, b := range books {
		prices = append(prices, b.Price)
		stock = append(stock, b.Stock)
	}
	fmt.Println("sum of prices:", Sum(prices)) // N = catalog.Money
	fmt.Println("books in stock:", Sum(stock)) // N = int

	priciest, _ := MaxBy(books, func(b catalog.Book) catalog.Money { return b.Price })
	longest, _ := MaxBy(books, func(b catalog.Book) int { return len(b.Title) })
	fmt.Println("most expensive:", priciest.Title, "| longest title:", longest.Title)

	byISBN := IndexBy(books, func(b catalog.Book) string { return b.ISBN })
	fmt.Println("9780141439686 ->", byISBN["9780141439686"].Title)

	p := paging.Of(books, 2, 4)
	fmt.Printf("page %d/%d, %d of %d items, len=%d cap=%d\n",
		p.Page, p.TotalPages, len(p.Items), p.TotalItems, len(p.Items), cap(p.Items))
	for _, b := range p.Items {
		fmt.Println("  ", b.Title)
	}
	empty := paging.Of(books, 9, 4)
	fmt.Printf("page 9: %d items, nil=%t\n", len(empty.Items), empty.Items == nil)

	fmt.Printf("%T\n%T\n", p, paging.Of([]string{"a"}, 1, 10))
}
