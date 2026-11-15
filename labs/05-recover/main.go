// Lab 05: what a panic in a handler does with and without httpapi.Recover.
package main

import (
	"log"
	"log/slog"
	"net/http"

	"bookstore/internal/httpapi"
)

func main() {
	broken := http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		panic("price list not loaded") // a programmer error, not a request error
	})

	mux := http.NewServeMux()
	mux.Handle("GET /plain", broken)
	mux.Handle("GET /safe", httpapi.Recover(slog.Default())(broken)) // since post 13 Recover takes a logger

	addr := "127.0.0.1:52711"
	log.Printf("lab listening on http://%s", addr)
	log.Fatal(http.ListenAndServe(addr, mux))
}
