// Command bookstore-api serves the Bookstore HTTP API.
package main

import (
	"encoding/json"
	"log"
	"net/http"

	"bookstore/internal/catalog"
)

func main() {
	var store catalog.Store

	http.HandleFunc("GET /hello", func(w http.ResponseWriter, r *http.Request) {
		w.Write([]byte("Hello from Bookstore API\n"))
	})
	http.HandleFunc("GET /api/books", func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(store.All())
	})

	addr := "127.0.0.1:52710"
	log.Printf("bookstore-api listening on http://%s", addr)
	log.Fatal(http.ListenAndServe(addr, nil))
}
