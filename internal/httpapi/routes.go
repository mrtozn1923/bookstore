package httpapi

import (
	"encoding/json"
	"errors"
	"fmt"
	"log"
	"net/http"
	"strconv"

	"bookstore/internal/catalog"
	"bookstore/internal/paging"
)

// API holds what the handlers need. main builds it once.
type API struct {
	Books catalog.Store
}

// Routes registers every endpoint on a new ServeMux.
func (a *API) Routes() *http.ServeMux {
	mux := http.NewServeMux()
	mux.HandleFunc("GET /hello", a.hello)
	mux.HandleFunc("GET /api/books", a.listBooks)
	mux.HandleFunc("GET /api/books/{isbn}", a.getBook)
	mux.HandleFunc("GET /api/authors", a.listAuthors)
	return mux
}

func (a *API) hello(w http.ResponseWriter, r *http.Request) {
	if _, err := fmt.Fprintln(w, "Hello from Bookstore API"); err != nil {
		log.Printf("write hello: %v", err)
	}
}

func (a *API) listBooks(w http.ResponseWriter, r *http.Request) {
	q := r.URL.Query()
	list := a.Books.All()
	if s := q.Get("maxPrice"); s != "" {
		limit, err := strconv.ParseInt(s, 10, 64)
		if err != nil {
			http.Error(w, "maxPrice must be a whole number of kurus", http.StatusBadRequest)
			return
		}
		list = a.Books.CheaperThan(catalog.Money(limit))
	}
	page, okPage := positiveInt(q.Get("page"), 1)
	size, okSize := positiveInt(q.Get("pageSize"), 20)
	if !okPage || !okSize || size > 100 {
		http.Error(w, "page and pageSize must be positive whole numbers, pageSize at most 100", http.StatusBadRequest)
		return
	}
	writeJSON(w, paging.Of(list, page, size))
}

func (a *API) getBook(w http.ResponseWriter, r *http.Request) {
	book, err := a.Books.ByISBN(r.PathValue("isbn"))
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
}

// authorSummary is one entry of GET /api/authors.
type authorSummary struct {
	Name   string   `json:"name"`
	Titles []string `json:"titles"`
}

func (a *API) listAuthors(w http.ResponseWriter, r *http.Request) {
	authors := []authorSummary{}
	for name, books := range a.Books.ByAuthor() {
		titles := make([]string, 0, len(books))
		for _, b := range books {
			titles = append(titles, b.Title)
		}
		authors = append(authors, authorSummary{Name: name, Titles: titles})
	}
	writeJSON(w, authors)
}

// positiveInt parses s as a whole number >= 1. An empty s gives def.
func positiveInt(s string, def int) (int, bool) {
	if s == "" {
		return def, true
	}
	n, err := strconv.Atoi(s)
	if err != nil || n < 1 {
		return 0, false
	}
	return n, true
}

// writeJSON writes v as the response body. Once the body has started there
// is no way to change the status code, so an encoding error is only logged.
func writeJSON(w http.ResponseWriter, v any) {
	w.Header().Set("Content-Type", "application/json")
	if err := json.NewEncoder(w).Encode(v); err != nil {
		log.Printf("write json: %v", err)
	}
}
