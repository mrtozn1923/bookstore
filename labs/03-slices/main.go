// Lab 03: arrays, slices, len/cap, append and shared backing arrays.
package main

import (
	"fmt"
	"slices"
)

func main() {
	shelf := [4]string{"Dune", "Emma", "The Hobbit", "Middlemarch"}
	copied := shelf
	copied[0] = "Changed"
	fmt.Println(shelf[0], copied[0])

	all := shelf[:] // slice over the whole array
	firstTwo := all[:2]
	fmt.Println(len(firstTwo), cap(firstTwo))

	firstTwo[1] = "Persuasion"
	fmt.Println(shelf[1])

	firstTwo = append(firstTwo, "Ulysses")
	fmt.Println(shelf[2], len(firstTwo), cap(firstTwo))

	firstTwo = append(firstTwo, "Beloved", "Dracula")
	firstTwo[0] = "Frankenstein"
	fmt.Println(shelf[0], len(firstTwo), cap(firstTwo))

	var titles []string
	for i := range 10 {
		titles = append(titles, fmt.Sprint(i))
		fmt.Print(cap(titles), " ")
	}
	fmt.Println()

	prices := []int{34990, 28950, 31000, 19900}
	sorted := slices.Clone(prices)
	slices.Sort(sorted)
	fmt.Println(prices, sorted, slices.Contains(prices, 19900), slices.Index(prices, 31000))
}
