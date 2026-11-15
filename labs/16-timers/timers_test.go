// Lab 16: time.After inside a loop is an idle timeout, not a deadline.
package timers

import (
	"context"
	"testing"
	"testing/synctest"
	"time"
)

// receiveAfter starts a new timer on every iteration.
func receiveAfter(ch <-chan int, timeout time.Duration) int {
	n := 0
	for {
		select {
		case _, ok := <-ch:
			if !ok {
				return n
			}
			n++
		case <-time.After(timeout): // reset by every message
			return n
		}
	}
}

// receiveDeadline uses one timer for the whole loop.
func receiveDeadline(ch <-chan int, timeout time.Duration) int {
	deadline := time.NewTimer(timeout)
	defer deadline.Stop()
	n := 0
	for {
		select {
		case _, ok := <-ch:
			if !ok {
				return n
			}
			n++
		case <-deadline.C:
			return n
		}
	}
}

// stockUpdates sends 10 values, one every 400ms, then closes the channel.
// It stops early when ctx is cancelled, so a receiver that gives up does
// not leave it blocked on the send forever.
func stockUpdates(ctx context.Context) <-chan int {
	ch := make(chan int)
	go func() {
		defer close(ch)
		tick := time.NewTicker(400 * time.Millisecond)
		defer tick.Stop()
		for i := range 10 {
			select {
			case <-tick.C:
			case <-ctx.Done():
				return
			}
			select {
			case ch <- i:
			case <-ctx.Done():
				return
			}
		}
	}()
	return ch
}

func TestTimeouts(t *testing.T) {
	for _, c := range []struct {
		name    string
		receive func(<-chan int, time.Duration) int
	}{{"time.After", receiveAfter}, {"deadline", receiveDeadline}} {
		name, receive := c.name, c.receive
		synctest.Test(t, func(t *testing.T) {
			ctx, cancel := context.WithCancel(context.Background())
			defer cancel()
			start := time.Now() // the fake clock of the bubble
			n := receive(stockUpdates(ctx), time.Second)
			t.Logf("%-10s got %2d updates in %v", name, n, time.Since(start))
		})
	}
}
