package inventory

import (
	"context"
	"errors"
	"net/http"
	"net/http/httptest"
	"sync/atomic"
	"testing"
	"time"
)

// stub answers like Bookstore.Api; fail lists the status codes of the
// first answers.
func stub(t *testing.T, fail ...int) (*Client, *atomic.Int32) {
	t.Helper()
	var calls atomic.Int32
	mux := http.NewServeMux()
	mux.HandleFunc("GET /api/stock/{isbn}", func(w http.ResponseWriter, r *http.Request) {
		if n := int(calls.Add(1)); n <= len(fail) {
			w.Header().Set("Retry-After", "0")
			http.Error(w, "try again", fail[n-1])
			return
		}
		if r.PathValue("isbn") != "9780441172719" {
			http.NotFound(w, r)
			return
		}
		w.Header().Set("Content-Type", "application/json")
		w.Write([]byte(`{"isbn":"9780441172719","available":12,"warehouse":"IST-1"}`))
	})
	srv := httptest.NewServer(mux)
	t.Cleanup(srv.Close)
	c := NewClient(srv.URL)
	c.Backoff = time.Millisecond
	return c, &calls
}

func TestStock(t *testing.T) {
	tests := []struct {
		name      string
		fail      []int
		wantErr   error
		wantCode  int
		wantCalls int32
	}{
		{"ok", nil, nil, 0, 1},
		{"two 503 then ok", []int{503, 503}, nil, 0, 3},
		{"429 then ok", []int{429}, nil, 0, 2},
		{"three 503: retries used up", []int{503, 503, 503}, nil, 503, 3},
		{"400 is not retried", []int{400}, nil, 400, 1},
	}
	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			c, calls := stub(t, tt.fail...)
			lvl, err := c.Stock(context.Background(), "9780441172719")
			var se *StatusError
			switch {
			case tt.wantCode != 0:
				if !errors.As(err, &se) || se.Code != tt.wantCode {
					t.Fatalf("err = %v, want status %d", err, tt.wantCode)
				}
			case err != nil || lvl.Available != 12:
				t.Fatalf("Stock() = %+v, %v", lvl, err)
			}
			if got := calls.Load(); got != tt.wantCalls {
				t.Errorf("calls = %d, want %d", got, tt.wantCalls)
			}
		})
	}
}

func TestStockNotFound(t *testing.T) {
	c, _ := stub(t)
	if _, err := c.Stock(context.Background(), "9780000000002"); !errors.Is(err, ErrNotFound) {
		t.Fatalf("err = %v, want ErrNotFound", err)
	}
}

func TestStockStopsWhenContextEnds(t *testing.T) {
	var calls atomic.Int32
	srv := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		calls.Add(1)
		<-r.Context().Done() // never answers
	}))
	defer srv.Close()
	c := NewClient(srv.URL)
	c.HTTP.Timeout = 50 * time.Millisecond
	c.Backoff = time.Hour // the context ends first

	ctx, cancel := context.WithTimeout(context.Background(), 300*time.Millisecond)
	defer cancel()
	start := time.Now()
	_, err := c.Stock(ctx, "9780441172719")
	if !errors.Is(err, context.DeadlineExceeded) || calls.Load() != 1 {
		t.Fatalf("err = %v after %d calls", err, calls.Load())
	}
	if d := time.Since(start); d > time.Second {
		t.Errorf("took %v, the context allowed 300ms", d)
	}
}
