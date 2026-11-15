package httpapi

import (
	"context"
	"log/slog"
	"net/http"
	"sync/atomic"
	"time"
)

// Health serves the liveness and readiness endpoints.
type Health struct {
	// Check reports whether a dependency the API needs is reachable, for
	// example the database. Nil means there is nothing to check.
	Check func(ctx context.Context) error

	draining atomic.Bool
}

// Drain makes Ready fail, so the load balancer stops sending new requests
// while the server finishes the ones it already has.
func (h *Health) Drain() { h.draining.Store(true) }

// Live answers as long as the process can serve HTTP at all.
func (h *Health) Live(w http.ResponseWriter, r *http.Request) {
	w.Write([]byte("ok\n"))
}

// Ready answers 200 only when the API can do useful work.
func (h *Health) Ready(w http.ResponseWriter, r *http.Request) {
	if h.draining.Load() {
		http.Error(w, "shutting down", http.StatusServiceUnavailable)
		return
	}
	if h.Check != nil {
		ctx, cancel := context.WithTimeout(r.Context(), 2*time.Second)
		defer cancel()
		if err := h.Check(ctx); err != nil {
			slog.WarnContext(ctx, "readiness check failed", "error", err)
			http.Error(w, "not ready", http.StatusServiceUnavailable)
			return
		}
	}
	w.Write([]byte("ready\n"))
}
