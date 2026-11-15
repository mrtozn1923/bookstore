//go:build go1.27

// Command 14-uuid tries the uuid package that arrived in Go 1.27.
package main

import (
	"fmt"
	"uuid"
)

func main() {
	a, b := uuid.NewV7(), uuid.NewV7() // time-ordered: good as a database key
	fmt.Println(a)
	fmt.Println(b)
	fmt.Println("v7 sorts by creation time:", a.Compare(b) < 0)
	fmt.Println("v4:", uuid.NewV4())

	if _, err := uuid.Parse("ORD-1001"); err != nil {
		fmt.Println("parse:", err)
	}
}
