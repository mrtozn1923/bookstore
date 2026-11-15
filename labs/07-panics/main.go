// Lab 07: channel operations that panic, and a panic inside a goroutine.
package main

import (
	"fmt"
	"os"
	"time"
)

func try(name string, f func()) {
	defer func() { fmt.Printf("%-16s %v\n", name, recover()) }()
	f()
}

func main() {
	try("send on closed", func() {
		ch := make(chan int, 1)
		close(ch)
		ch <- 1
	})
	try("close twice", func() {
		ch := make(chan int)
		close(ch)
		close(ch)
	})
	try("close nil", func() {
		var ch chan int
		close(ch)
	})

	if len(os.Args) > 1 && os.Args[1] == "crash" {
		// recover in main cannot catch a panic in another goroutine.
		defer func() { fmt.Println("main recovered:", recover()) }()
		go func() {
			var prices map[string]int
			//lint:ignore SA5000 the nil map write is the point of this lab
			prices["9780441172719"] = 34990 // nil map: panics in this goroutine
		}()
		time.Sleep(100 * time.Millisecond)
		fmt.Println("never printed")
	}
}
