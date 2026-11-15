//go:build go1.27

// Lab 08: finding the goroutine leak from post 07 with the goroutineleak
// profile, generally available in Go 1.27.
package main

import (
	"fmt"
	"os"
	"runtime/pprof"
	"time"

	"bookstore/internal/catalog"
)

// firstMissing is the leaky function from labs/07-leak.
func firstMissing(isbns []string) error {
	errs := make(chan error)
	for _, isbn := range isbns {
		go func() {
			_, err := catalog.Store{}.ByISBN(isbn)
			if err == nil {
				time.Sleep(50 * time.Millisecond)
			}
			errs <- err
		}()
	}
	for range isbns {
		if err := <-errs; err != nil {
			return err
		}
	}
	return nil
}

func main() {
	cart := []string{"9780000000000", "9780441172719", "9780547928227", "9780141439549"}
	for range 3 {
		_ = firstMissing(cart)
	}
	time.Sleep(100 * time.Millisecond) // let the senders reach their blocked send

	if err := pprof.Lookup("goroutineleak").WriteTo(os.Stdout, 1); err != nil {
		fmt.Println(err)
	}
}
