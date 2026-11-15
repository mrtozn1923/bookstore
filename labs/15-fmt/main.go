// Lab 15: the fmt verbs a C# developer reaches for first.
package main

import (
	"errors"
	"fmt"

	"bookstore/internal/catalog"
	"bookstore/internal/orders"
)

func main() {
	b := catalog.Book{ISBN: "9780441172719", Title: "Dune", Author: "Frank Herbert", Price: 34990, Stock: 12}
	fmt.Printf("%v\n", b)
	fmt.Printf("%+v\n", b)
	fmt.Printf("%#v\n", b)
	fmt.Printf("%T %T\n", b, b.Price)

	// Money has a String method: %v and %s use it, %d does not.
	fmt.Printf("%v | %s | %d\n", b.Price, b.Price, b.Price)
	fmt.Printf("%q %x % x\n", b.Title, b.Title, "Dü")
	fmt.Printf("[%-12s][%15s][%6.2f][%05d]\n", b.Title, b.Author, 349.9, 42)
	fmt.Printf("%v %d\n", orders.StatusPaid, orders.StatusPaid)

	err := fmt.Errorf("place order: %w", orders.ErrOutOfStock)
	fmt.Println(err, errors.Is(err, orders.ErrOutOfStock))
	s := fmt.Sprintf("%s has %d in stock", b.Title, b.Stock)
	fmt.Println(s)
}
