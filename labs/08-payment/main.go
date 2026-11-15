// Lab 08: a time-dependent rule, tested with testing/synctest in main_test.go.
package main

import (
	"context"
	"fmt"
	"time"
)

// awaitPayment waits until paid is closed or timeout has passed.
func awaitPayment(ctx context.Context, paid <-chan struct{}, timeout time.Duration) error {
	ctx, cancel := context.WithTimeout(ctx, timeout)
	defer cancel()
	select {
	case <-paid:
		return nil
	case <-ctx.Done():
		return fmt.Errorf("await payment: %w", ctx.Err())
	}
}

func main() {
	start := time.Now()
	err := awaitPayment(context.Background(), make(chan struct{}), 200*time.Millisecond)
	fmt.Println(err, time.Since(start).Round(10*time.Millisecond))
}
