// Lab 17: map iteration order, and the functions that make it stable.
package main

import (
	"fmt"
	"maps"
	"slices"
)

func main() {
	stock := map[string]int{"Dune": 12, "Emma": 7, "Foundation": 9, "Middlemarch": 4, "Persuasion": 3}
	for range 3 {
		var order []string
		for title := range stock {
			order = append(order, title)
		}
		fmt.Println(order)
	}

	fmt.Println(slices.Sorted(maps.Keys(stock))) // keys returns an iterator, Sorted collects it
	fmt.Println(slices.Collect(maps.Values(map[string]int{"Dune": 12})))

	low := maps.Clone(stock)
	maps.DeleteFunc(low, func(_ string, n int) bool { return n >= 5 })
	fmt.Println(low, len(stock))
}
