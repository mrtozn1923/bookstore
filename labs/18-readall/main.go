// Lab 18: the same count with os.ReadFile, for comparison.
package main

import (
	"bytes"
	"fmt"
	"os"
	"runtime"
	"time"
)

func main() {
	start := time.Now()
	data, err := os.ReadFile(os.Args[1]) // the whole file in memory
	if err != nil {
		fmt.Fprintln(os.Stderr, err)
		os.Exit(1)
	}
	orders := 0
	for _, line := range bytes.Split(data, []byte("\n")) { // plus a slice header per line
		if bytes.Contains(line, []byte(`"msg":"order placed"`)) {
			orders++
		}
	}
	var m runtime.MemStats
	runtime.ReadMemStats(&m)
	fmt.Printf("%d orders in %v, memory from the OS: %d MB\n", orders, time.Since(start).Round(100*time.Millisecond), m.Sys>>20)
}
