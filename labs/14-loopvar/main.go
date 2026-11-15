// Command 14-loopvar runs the same loop with the loop variable semantics of
// Go 1.22+ (this file) and of Go 1.21 (old.go).
package main

import "fmt"

func main() {
	fmt.Println("go1.22+:", titles())
	fmt.Println("go1.21: ", titlesGo121())
}

var catalog = []string{"Dune", "Emma", "Middlemarch"}

// titles collects one closure per book and calls them after the loop.
func titles() []string {
	var fs []func() string
	for _, t := range catalog {
		fs = append(fs, func() string { return t })
	}
	var out []string
	for _, f := range fs {
		out = append(out, f())
	}
	return out
}
