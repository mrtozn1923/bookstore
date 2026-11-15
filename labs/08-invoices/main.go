// Lab 08: rendering 10,000 invoices with errgroup, a limit and cancellation.
package main

import (
	"context"
	"fmt"
	"sync/atomic"
	"time"

	"golang.org/x/sync/errgroup"
)

// render stands in for producing one invoice PDF, which takes about 1ms.
func render(ctx context.Context, orderID, failAt int) error {
	if err := ctx.Err(); err != nil {
		return err // another invoice failed: do not start new work
	}
	time.Sleep(time.Millisecond)
	if orderID == failAt {
		return fmt.Errorf("render invoice ORD-%d: template not found", orderID)
	}
	return nil
}

func run(name string, limit, failAt int) {
	g, ctx := errgroup.WithContext(context.Background())
	g.SetLimit(limit) // -1 means no limit

	var rendered, inFlight, peak atomic.Int64
	start := time.Now()
	for id := 1; id <= 10_000; id++ {
		g.Go(func() error { // blocks while limit goroutines are running
			n := inFlight.Add(1)
			defer inFlight.Add(-1)
			for { // record the highest number of invoices rendered at once
				p := peak.Load()
				if n <= p || peak.CompareAndSwap(p, n) {
					break
				}
			}
			if err := render(ctx, id, failAt); err != nil {
				return err
			}
			rendered.Add(1)
			return nil
		})
	}
	err := g.Wait() // the first error, after every goroutine has returned
	fmt.Printf("%-12s rendered=%5d peak=%5d time=%-7v err=%v\n", name, rendered.Load(), peak.Load(),
		time.Since(start).Round(time.Millisecond), err)
}

func main() {
	run("limit 8", 8, 0)
	run("limit 8+err", 8, 5000)
	run("no limit", -1, 0)
}
