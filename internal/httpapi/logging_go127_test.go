//go:build go1.27

package httpapi_test

import (
	"bytes"
	"log/slog"
	"net/http"
	"net/http/httptest"
	"strings"
	"testing"
	"testing/synctest"
	"time"

	"bookstore/internal/httpapi"
)

// TestLoggingDuration runs a slow handler inside a synctest bubble. The fake
// clock makes the logged duration exact, and NewTestServer (Go 1.27) serves
// over an in-memory network that works inside the bubble.
func TestLoggingDuration(t *testing.T) {
	var buf bytes.Buffer
	logger := slog.New(httpapi.NewLogHandler(slog.NewTextHandler(&buf, &slog.HandlerOptions{
		ReplaceAttr: func(_ []string, a slog.Attr) slog.Attr {
			if a.Key == slog.TimeKey {
				return slog.Attr{} // drop the time: the bubble's clock starts in 2000
			}
			return a
		},
	})))

	synctest.Test(t, func(t *testing.T) {
		slow := http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			time.Sleep(2 * time.Second) // returns at once: only the fake clock moves
			w.Write([]byte("ok\n"))
		})
		srv := httptest.NewTestServer(t, httpapi.Chain(slow, httpapi.RequestID, httpapi.Logging(logger)))

		req, _ := http.NewRequest(http.MethodGet, "http://bookstore.test/slow", nil)
		req.Header.Set("X-Request-ID", "test-rid")
		resp, err := srv.Client().Do(req)
		if err != nil {
			t.Fatal(err)
		}
		resp.Body.Close()
		synctest.Wait() // let the Logging middleware finish its line
	})

	want := "level=INFO msg=request method=GET path=/slow status=200 bytes=3 duration_ms=2000 request_id=test-rid"
	if got := strings.TrimSpace(buf.String()); got != want {
		t.Errorf("log line = %q, want %q", got, want)
	}
}
