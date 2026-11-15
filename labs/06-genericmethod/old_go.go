//go:build !go1.27

package main

import "fmt"

func main() {
	fmt.Println("this lab needs Go 1.27 or later (generic methods)")
}
