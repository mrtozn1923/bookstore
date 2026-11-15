// Package httpapi holds the HTTP handlers and middleware of the Bookstore API.
package httpapi

import (
	"fmt"
	"log/slog"
	"net/http"
	"runtime/debug"
)

// Recover turns a panic in next into a 500 response and one error record
// with the stack trace of the goroutine that panicked.
func Recover(logger *slog.Logger) Middleware {
	return func(next http.Handler) http.Handler {
		return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			defer func() {
				v := recover()
				if v == nil {
					return
				}
				if v == http.ErrAbortHandler {
					panic(v) // net/http uses this value to abort a response on purpose
				}
				logger.ErrorContext(r.Context(), "panic",
					"method", r.Method,
					"path", r.URL.Path,
					"error", fmt.Sprint(v),
					"stack", string(debug.Stack()),
				)
				writeProblem(w, r, Problem{Status: http.StatusInternalServerError})
			}()
			next.ServeHTTP(w, r)
		})
	}
}
