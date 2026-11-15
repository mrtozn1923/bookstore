// Lab 03: maps, iteration order, pointers, switch and defer.
package main

import (
	"fmt"
	"maps"
	"slices"
)

type Book struct {
	Title string
	Stock int
}

func restock(b *Book, n int) {
	b.Stock += n
}

func restockCopy(b Book, n int) {
	b.Stock += n
}

func label(stock int) string {
	switch {
	case stock == 0:
		return "out of stock"
	case stock < 5:
		return "low"
	default:
		return "ok"
	}
}

func main() {
	defer fmt.Println("main finished")

	stock := map[string]int{"Dune": 12, "Emma": 7, "The Hobbit": 0, "Middlemarch": 4}
	for range 3 {
		for title := range stock {
			fmt.Print(title, " | ")
		}
		fmt.Println()
	}
	for _, title := range slices.Sorted(maps.Keys(stock)) {
		fmt.Printf("%s=%s ", title, label(stock[title]))
	}
	fmt.Println()

	n, ok := stock["Ulysses"]
	fmt.Println(n, ok)

	b := Book{Title: "Emma", Stock: 7}
	restockCopy(b, 5)
	fmt.Println(b.Stock)
	restock(&b, 5)
	fmt.Println(b.Stock)

	var reserved map[string]int
	fmt.Println(reserved["Dune"], len(reserved))
	//lint:ignore SA5000 the panic is the point of this lab
	reserved["Dune"] = 1
}
