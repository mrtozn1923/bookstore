// Command bookstore-api serves the Bookstore HTTP API.
package main

import (
	"encoding/json"
	"errors"
	"fmt"
	"log"
	"net/http"
	"strconv"

	"bookstore/internal/catalog"
	"bookstore/internal/httpapi"
)

func main() {
	var store catalog.Store
	mux := http.NewServeMux()

	mux.HandleFunc("GET /hello", func(w http.ResponseWriter, r *http.Request) {
		if _, err := fmt.Fprintln(w, "Hello from Bookstore API"); err != nil {
			log.Printf("write hello: %v", err)
		}
	})
	mux.HandleFunc("GET /api/books", func(w http.ResponseWriter, r *http.Request) {
		list := store.All()
		if s := r.URL.Query().Get("maxPrice"); s != "" {
			limit, err := strconv.ParseInt(s, 10, 64)
			if err != nil {
				http.Error(w, "maxPrice must be a whole number of kurus", http.StatusBadRequest)
				return
			}
			list = store.CheaperThan(catalog.Money(limit))
		}
		writeJSON(w, list)
	})
	mux.HandleFunc("GET /api/books/{isbn}", func(w http.ResponseWriter, r *http.Request) {
		book, err := store.ByISBN(r.PathValue("isbn"))
		if errors.Is(err, catalog.ErrBookNotFound) {
			http.Error(w, err.Error(), http.StatusNotFound)
			return
		}
		if err != nil {
			log.Printf("get book: %v", err)
			http.Error(w, "internal server error", http.StatusInternalServerError)
			return
		}
		writeJSON(w, book)
	})

	addr := "127.0.0.1:52710"
	log.Printf("bookstore-api listening on http://%s", addr)
	log.Fatal(http.ListenAndServe(addr, httpapi.Recover(mux)))
}

// writeJSON writes v as the response body. Once the body has started there
// is no way to change the status code, so an encoding error is only logged.
func writeJSON(w http.ResponseWriter, v any) {
	w.Header().Set("Content-Type", "application/json")
	if err := json.NewEncoder(w).Encode(v); err != nil {
		log.Printf("write json: %v", err)
	}
}
