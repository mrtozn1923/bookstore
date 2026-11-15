// Lab 17: functions that hand back a slice can share or clear its array.
package main

import (
	"fmt"
	"slices"
)

// withBestseller appends to the slice it was given.
func withBestseller(shelf []string) []string {
	return append(shelf, "Dune")
}

func main() {
	shelf := make([]string, 2, 4)
	copy(shelf, []string{"Emma", "Persuasion"})
	a := withBestseller(shelf)
	b := append(shelf, "Foundation") // same backing array: overwrites a[2]
	fmt.Println(a, b)

	c := withBestseller(slices.Clip(shelf)) // cap == len: append must copy
	d := append(slices.Clip(shelf), "Foundation")
	fmt.Println(c, d)

	titles := []string{"Dune", "Emma", "Middlemarch", "Persuasion"}
	rest := slices.Delete(titles, 1, 2) // removes "Emma"
	fmt.Printf("%q %q\n", rest, titles) // the old tail is zeroed (Go 1.22)

	tags := []string{"classic", "classic", "romance", "classic"}
	fmt.Println(slices.Compact(slices.Clone(tags))) // only neighbours
	slices.Sort(tags)
	fmt.Println(slices.Compact(tags)) // sorted first: real distinct
	fmt.Println(slices.Insert([]string{"Dune", "Emma"}, 1, "Foundation"))
}
