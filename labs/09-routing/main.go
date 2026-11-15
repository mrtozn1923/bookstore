// Lab 09: ServeMux patterns, precedence, wildcards and redirects.
package main

import (
	"fmt"
	"log"
	"net/http"
	"os"
)

func reply(name string) http.HandlerFunc {
	return func(w http.ResponseWriter, r *http.Request) {
		fmt.Fprintf(w, "%s isbn=%q rest=%q\n", name, r.PathValue("isbn"), r.PathValue("rest"))
	}
}

func main() {
	mux := http.NewServeMux()
	mux.Handle("GET /api/books/{isbn}", reply("book"))
	mux.Handle("GET /api/books/bestsellers", reply("bestsellers"))    // more specific: wins
	mux.Handle("GET /covers/{rest...}", reply("cover"))               // matches the rest of the path
	mux.Handle("GET /docs/", reply("docs"))                           // trailing slash: a subtree
	mux.Handle("GET /api/books/{isbn}/reviews/{$}", reply("reviews")) // {$}: exact match only

	if len(os.Args) > 1 && os.Args[1] == "conflict" {
		// Neither pattern is more specific than the other: Handle panics.
		mux.Handle("GET /api/{kind}/9780441172719", reply("conflict"))
	}

	addr := "127.0.0.1:52711"
	log.Printf("routing lab on http://%s", addr)
	log.Fatal(http.ListenAndServe(addr, mux))
}
