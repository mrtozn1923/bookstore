// Package httpapi holds HTTP helpers shared by the Bookstore commands.
package httpapi

import (
	"log"
	"net/http"
)

// Recover turns a panic in next into a 500 response and one log line.
func Recover(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		defer func() {
			v := recover()
			if v == nil {
				return
			}
			if v == http.ErrAbortHandler {
				panic(v) // net/http uses this value to abort a response on purpose
			}
			log.Printf("panic serving %s %s: %v", r.Method, r.URL.Path, v)
			http.Error(w, "internal server error", http.StatusInternalServerError)
		}()
		next.ServeHTTP(w, r)
	})
}
