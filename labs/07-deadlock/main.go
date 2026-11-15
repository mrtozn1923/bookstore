// Lab 07: an unbuffered send with nobody receiving.
package main

import "fmt"

func main() {
	stock := make(chan int)
	stock <- 12 // blocks until another goroutine receives
	fmt.Println(<-stock)
}
