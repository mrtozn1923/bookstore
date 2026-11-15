// Command bookstore-api serves the Bookstore HTTP API.
package main

import (
	"encoding/json"
	"log"
	"net/http"
	"strconv"

	"bookstore/internal/catalog"
)

func main() {
	var store catalog.Store

	http.HandleFunc("GET /hello", func(w http.ResponseWriter, r *http.Request) {
		w.Write([]byte("Hello from Bookstore API\n"))
	})
	http.HandleFunc("GET /api/books", func(w http.ResponseWriter, r *http.Request) {
		list := store.All()
		if s := r.URL.Query().Get("maxPrice"); s != "" {
			limit, err := strconv.ParseInt(s, 10, 64)
			if err != nil {
				http.Error(w, "maxPrice must be a whole number of kurus", http.StatusBadRequest)
				return
			}
			list = store.CheaperThan(catalog.Money(limit))
		}
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(list)
	})

	addr := "127.0.0.1:52710"
	log.Printf("bookstore-api listening on http://%s", addr)
	log.Fatal(http.ListenAndServe(addr, nil))
}
