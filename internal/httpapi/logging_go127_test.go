//go:build go1.27

package httpapi_test

import (
	"bytes"
	"log"
	"net/http"
	"net/http/httptest"
	"os"
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
	log.SetOutput(&buf)
	log.SetFlags(0)
	t.Cleanup(func() { log.SetOutput(os.Stderr); log.SetFlags(log.LstdFlags) })

	synctest.Test(t, func(t *testing.T) {
		slow := http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			time.Sleep(2 * time.Second) // returns at once: only the fake clock moves
			w.Write([]byte("ok\n"))
		})
		srv := httptest.NewTestServer(t, httpapi.Chain(slow, httpapi.RequestID, httpapi.Logging))

		req, _ := http.NewRequest(http.MethodGet, "http://bookstore.test/slow", nil)
		req.Header.Set("X-Request-ID", "test-rid")
		resp, err := srv.Client().Do(req)
		if err != nil {
			t.Fatal(err)
		}
		resp.Body.Close()
		synctest.Wait() // let the Logging middleware finish its line
	})

	if got, want := strings.TrimSpace(buf.String()), "GET /slow 200 3B 2s rid=test-rid"; got != want {
		t.Errorf("log line = %q, want %q", got, want)
	}
}
