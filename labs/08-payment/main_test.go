package main

import (
	"context"
	"errors"
	"testing"
	"testing/synctest"
	"time"
)

func TestAwaitPaymentTimesOut(t *testing.T) {
	synctest.Test(t, func(t *testing.T) {
		start := time.Now() // fake clock inside the bubble
		err := awaitPayment(t.Context(), make(chan struct{}), 15*time.Minute)
		if !errors.Is(err, context.DeadlineExceeded) {
			t.Fatalf("err = %v, want DeadlineExceeded", err)
		}
		if waited := time.Since(start); waited != 15*time.Minute {
			t.Fatalf("waited %v, want exactly 15m", waited)
		}
	})
}

func TestAwaitPaymentPaidInTime(t *testing.T) {
	synctest.Test(t, func(t *testing.T) {
		paid := make(chan struct{})
		go func() {
			time.Sleep(14 * time.Minute)
			close(paid)
		}()
		if err := awaitPayment(t.Context(), paid, 15*time.Minute); err != nil {
			t.Fatalf("err = %v, want nil", err)
		}
	})
}
