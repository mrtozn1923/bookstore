// Lab 07: what 100,000 parked goroutines cost.
package main

import (
	"fmt"
	"runtime"
	"sync"
	"time"
)

func main() {
	const n = 100_000
	fmt.Println("GOMAXPROCS:", runtime.GOMAXPROCS(0))

	var before runtime.MemStats
	runtime.ReadMemStats(&before)

	start := time.Now()
	release := make(chan struct{})
	var wg sync.WaitGroup
	for range n {
		wg.Add(1)
		go func() {
			defer wg.Done()
			<-release // park until main closes the channel
		}()
	}
	fmt.Printf("started %d goroutines in %v\n", n, time.Since(start).Round(time.Millisecond))

	var after runtime.MemStats
	runtime.ReadMemStats(&after)
	fmt.Println("goroutines:", runtime.NumGoroutine())
	fmt.Printf("stack in use: %.1f MB (%.1f KB per goroutine)\n",
		float64(after.StackInuse-before.StackInuse)/1e6,
		float64(after.StackInuse-before.StackInuse)/n/1e3)

	close(release) // one close wakes every receiver
	wg.Wait()
	fmt.Println("after close:", runtime.NumGoroutine())
}
