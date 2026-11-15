// Lab 07: returning early leaves workers blocked on a send forever.
package main

import (
	"errors"
	"fmt"
	"runtime"
	"time"

	"bookstore/internal/catalog"
)

// firstMissing starts one goroutine per ISBN and returns at the first error.
func firstMissing(isbns []string) error {
	errs := make(chan error) // unbuffered: each sender waits for a receiver
	for _, isbn := range isbns {
		go func() {
			_, err := catalog.Store{}.ByISBN(isbn)
			if err == nil {
				time.Sleep(50 * time.Millisecond) // known books also ask the inventory service
			}
			errs <- err
		}()
	}
	for range isbns {
		if err := <-errs; err != nil {
			return err // the remaining senders never get a receiver
		}
	}
	return nil
}

func main() {
	cart := []string{"9780000000000", "9780441172719", "9780547928227", "9780141439549"}
	for i := 1; i <= 3; i++ {
		err := firstMissing(cart)
		time.Sleep(100 * time.Millisecond)
		fmt.Printf("call %d: %v, goroutines=%d\n", i, errors.Unwrap(err), runtime.NumGoroutine())
	}
}
