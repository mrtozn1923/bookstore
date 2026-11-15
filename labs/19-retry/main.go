// Lab 19: the inventory client against a stock service that is restarting.
package main

import (
	"context"
	"fmt"
	"net"
	"net/http"
	"sync/atomic"
	"time"

	"bookstore/internal/inventory"
)

func main() {
	start := time.Now()
	var calls atomic.Int32
	mux := http.NewServeMux()
	mux.HandleFunc("GET /api/stock/{isbn}", func(w http.ResponseWriter, r *http.Request) {
		n := calls.Add(1)
		fmt.Printf("  server: call %d at %v\n", n, time.Since(start).Round(10*time.Millisecond))
		switch n {
		case 1:
			http.Error(w, "starting", http.StatusServiceUnavailable)
		case 2:
			w.Header().Set("Retry-After", "1") // seconds
			http.Error(w, "slow down", http.StatusTooManyRequests)
		default:
			fmt.Fprintf(w, `{"isbn":%q,"available":12,"warehouse":"IST-1"}`, r.PathValue("isbn"))
		}
	})
	ln, err := net.Listen("tcp", "127.0.0.1:52716")
	if err != nil {
		panic(err)
	}
	srv := &http.Server{Handler: mux, ReadHeaderTimeout: 5 * time.Second}
	go srv.Serve(ln)
	defer srv.Close()

	client := inventory.NewClient("http://127.0.0.1:52716")
	lvl, err := client.Stock(context.Background(), "9780441172719")
	fmt.Printf("%+v %v after %v\n", lvl, err, time.Since(start).Round(10*time.Millisecond))

	calls.Store(0)
	start = time.Now()
	ctx, cancel := context.WithTimeout(context.Background(), 500*time.Millisecond) // the caller's budget
	defer cancel()
	_, err = client.Stock(ctx, "9780441172719")
	fmt.Printf("%v after %v\n", err, time.Since(start).Round(10*time.Millisecond))
}
