// Lab 03: variables, constants, zero values, strings and runes.
package main

import (
	"fmt"
	"unicode/utf8"
)

type OrderStatus int

const (
	Pending OrderStatus = iota
	Paid
	Shipped
	Cancelled
)

type Book struct {
	ISBN     string
	Title    string
	Price    int64
	InStock  bool
	Tags     []string
	Discount *int
}

func main() {
	var count int
	var title string
	var b Book
	fmt.Printf("count=%d title=%q\n", count, title)
	fmt.Printf("%+v\n", b)
	fmt.Println(b.Tags == nil, len(b.Tags), b.Discount == nil)

	price := 349.90 // := infers float64
	isbn := "9780441172719"
	fmt.Printf("%T %T %v %v\n", price, isbn, Paid, Cancelled)

	turkish := "Çalıkuşu"
	fmt.Println(len(turkish), utf8.RuneCountInString(turkish))
	fmt.Println(turkish[0], string(turkish[0]))
	for i, r := range turkish[:4] {
		fmt.Printf("%d:%c ", i, r)
	}
	fmt.Println()

	b.Discount = new(15) // Go 1.26: new with an expression
	fmt.Println(*b.Discount)
}
