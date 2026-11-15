//go:build go1.27

// Lab 15: strings.CutLast (Go 1.27) next to strings.Cut.
package main

import (
	"fmt"
	"strings"
)

func main() {
	name := "orders.2026-11-14.csv"
	before, after, found := strings.Cut(name, ".")
	fmt.Printf("Cut:     %q %q %v\n", before, after, found)
	base, ext, found := strings.CutLast(name, ".")
	fmt.Printf("CutLast: %q %q %v\n", base, ext, found)
}
