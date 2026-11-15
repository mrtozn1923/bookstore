// Lab 07: unbuffered and buffered channels, close and range, select.
package main

import (
	"fmt"
	"time"
)

var start = time.Now()

func logf(format string, args ...any) {
	fmt.Printf("%4dms  "+format+"\n", append([]any{time.Since(start).Milliseconds()}, args...)...)
}

// reserve sends n reservation IDs and closes the channel when done.
func reserve(out chan<- string, n int) {
	for i := 1; i <= n; i++ {
		out <- fmt.Sprintf("RES-%d", i)
		logf("sent RES-%d", i)
	}
	close(out) // only the sender closes
}

func main() {
	// 1) Unbuffered: every send waits for a receive.
	unbuffered := make(chan string)
	go reserve(unbuffered, 3)
	time.Sleep(100 * time.Millisecond)
	for id := range unbuffered { // stops when the channel is closed and empty
		logf("got  %s", id)
		time.Sleep(100 * time.Millisecond)
	}

	// 2) Buffered: sends do not wait while there is room.
	start = time.Now()
	buffered := make(chan string, 3)
	go reserve(buffered, 3)
	time.Sleep(100 * time.Millisecond)
	for id := range buffered {
		logf("got  %s", id)
	}

	// 3) Receiving from a closed channel: zero value and ok == false.
	id, ok := <-buffered
	fmt.Printf("closed: %q %t\n", id, ok)

	// 4) select: wait for whichever is ready first, or give up.
	slow := make(chan string)
	go func() {
		time.Sleep(300 * time.Millisecond)
		slow <- "stock: 12"
	}()
	select {
	case v := <-slow:
		fmt.Println(v)
	case <-time.After(100 * time.Millisecond):
		fmt.Println("inventory did not answer in 100ms")
	}

	// 5) default makes select non-blocking.
	select {
	case v := <-slow:
		fmt.Println("late answer:", v)
	default:
		fmt.Println("nothing ready yet")
	}
	fmt.Println("late answer:", <-slow) // the goroutine is still waiting to send
}
