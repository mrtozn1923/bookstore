// The first version of the Bookstore API: one file, standard library only
package main

import (
	"encoding/json"
	"log"
	"net/http"
)

type book struct {
	ISBN  string `json:"isbn"`
	Title string `json:"title"`
}

var books = []book{
	{ISBN: "9780441172719", Title: "Dune"},
	{ISBN: "9780547928227", Title: "The Hobbit"},
	{ISBN: "9780141439549", Title: "Middlemarch"},
}

func main() {
	http.HandleFunc("GET /hello", func(w http.ResponseWriter, r *http.Request) {
		w.Write([]byte("Hello from Bookstore API\n"))
	})
	http.HandleFunc("GET /api/books", func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(books)
	})

	addr := "127.0.0.1:52710"
	log.Printf("bookstore-api listening on http://%s", addr)
	log.Fatal(http.ListenAndServe(addr, nil))
}
