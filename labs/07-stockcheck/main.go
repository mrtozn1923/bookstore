// Lab 07: checking stock for a cart with a pipeline, fan-out and fan-in.
package main

import (
	"fmt"
	"sync"
	"time"

	"bookstore/internal/catalog"
)

// lookup stands in for a call to the inventory service, which takes 50ms.
func lookup(isbn string) (catalog.Book, error) {
	time.Sleep(50 * time.Millisecond)
	return catalog.Store{}.ByISBN(isbn)
}

type result struct {
	isbn  string
	stock int
	err   error
}

// generate is the first stage: it turns a slice into a channel.
func generate(isbns []string) <-chan string {
	out := make(chan string)
	go func() {
		defer close(out)
		for _, isbn := range isbns {
			out <- isbn
		}
	}()
	return out
}

// check is a worker: it reads ISBNs until in is closed.
func check(in <-chan string) <-chan result {
	out := make(chan result)
	go func() {
		defer close(out)
		for isbn := range in {
			b, err := lookup(isbn)
			out <- result{isbn: isbn, stock: b.Stock, err: err}
		}
	}()
	return out
}

// merge is fan-in: it copies every input channel into one output channel.
func merge(ins ...<-chan result) <-chan result {
	out := make(chan result)
	var wg sync.WaitGroup
	for _, in := range ins {
		wg.Add(1)
		go func() {
			defer wg.Done()
			for r := range in {
				out <- r
			}
		}()
	}
	go func() {
		wg.Wait()
		close(out) // close once, after every copier is done
	}()
	return out
}

func main() {
	cart := []string{"9780441172719", "9780547928227", "9780141439549",
		"9780141439587", "9780141439686", "9780000000000"}

	start := time.Now()
	for _, isbn := range cart {
		if _, err := lookup(isbn); err != nil {
			fmt.Println("sequential:", err)
		}
	}
	fmt.Println("sequential:", time.Since(start).Round(10*time.Millisecond))

	start = time.Now()
	isbns := generate(cart)
	workers := []<-chan result{check(isbns), check(isbns), check(isbns)} // fan-out: 3 readers, one channel
	for r := range merge(workers...) {
		if r.err != nil {
			fmt.Println("  error:", r.err)
			continue
		}
		fmt.Printf("  %s stock=%d\n", r.isbn, r.stock)
	}
	fmt.Println("pipeline with 3 workers:", time.Since(start).Round(10*time.Millisecond))
}
