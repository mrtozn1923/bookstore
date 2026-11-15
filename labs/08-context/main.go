// Lab 08: cancellation, deadlines, causes and values with context.Context.
package main

import (
	"context"
	"errors"
	"fmt"
	"time"

	"bookstore/internal/catalog"
)

// requestIDKey is unexported, so no other package can collide with it.
type requestIDKey struct{}

// lookupStock stands in for an inventory call that takes delay.
// It gives up as soon as ctx is done.
func lookupStock(ctx context.Context, isbn string, delay time.Duration) (int, error) {
	rid, _ := ctx.Value(requestIDKey{}).(string)
	select {
	case <-time.After(delay):
		b, err := catalog.Store{}.ByISBN(isbn)
		return b.Stock, err
	case <-ctx.Done():
		return 0, fmt.Errorf("request %s: lookup %s: %w", rid, isbn, context.Cause(ctx))
	}
}

func main() {
	base := context.WithValue(context.Background(), requestIDKey{}, "req-42")

	// 1) and 2) A 100ms deadline: fast enough, then too slow.
	for _, delay := range []time.Duration{50 * time.Millisecond, 300 * time.Millisecond} {
		ctx, cancel := context.WithTimeout(base, 100*time.Millisecond)
		start := time.Now()
		stock, err := lookupStock(ctx, "9780441172719", delay)
		cancel() // always release the timer, even on success
		fmt.Printf("delay %v: stock=%d err=%v (%v)\n", delay, stock, err,
			time.Since(start).Round(10*time.Millisecond))
		fmt.Println("  deadline exceeded?", errors.Is(err, context.DeadlineExceeded))
	}

	// 3) Cancelling with a cause.
	ctx, cancel := context.WithCancelCause(base)
	go func() {
		time.Sleep(20 * time.Millisecond)
		cancel(errors.New("client closed the connection"))
	}()
	_, err := lookupStock(ctx, "9780141439587", time.Second)
	fmt.Println(err)
	fmt.Println("  ctx.Err():", ctx.Err())

	// 4) Cancellation flows down: a child of a cancelled context is done at once.
	child, stop := context.WithTimeout(ctx, time.Hour)
	defer stop()
	<-child.Done()
	fmt.Println("child:", child.Err(), "| cause:", context.Cause(child))
}
