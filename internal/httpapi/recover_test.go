package httpapi_test

import (
	"bytes"
	"encoding/json"
	"log/slog"
	"net/http"
	"net/http/httptest"
	"strings"
	"testing"

	"bookstore/internal/httpapi"
)

func TestRecoverLogsStack(t *testing.T) {
	var buf bytes.Buffer
	logger := slog.New(httpapi.NewLogHandler(slog.NewJSONHandler(&buf, nil)))
	boom := http.HandlerFunc(func(http.ResponseWriter, *http.Request) {
		panic("price list not loaded")
	})
	h := httpapi.Chain(boom, httpapi.RequestID, httpapi.Recover(logger))

	rec := httptest.NewRecorder()
	req := httptest.NewRequest(http.MethodGet, "/api/books", nil)
	req.Header.Set("X-Request-ID", "rid-1")
	h.ServeHTTP(rec, req)

	if rec.Code != http.StatusInternalServerError || rec.Header().Get("Content-Type") != "application/problem+json" {
		t.Fatalf("got %d %s, want 500 problem+json", rec.Code, rec.Header().Get("Content-Type"))
	}
	var entry struct {
		Msg       string `json:"msg"`
		Error     string `json:"error"`
		Stack     string `json:"stack"`
		RequestID string `json:"request_id"`
	}
	if err := json.Unmarshal(buf.Bytes(), &entry); err != nil {
		t.Fatal(err)
	}
	if entry.Msg != "panic" || entry.RequestID != "rid-1" || entry.Error != "price list not loaded" ||
		!strings.Contains(entry.Stack, "recover_test.go") {
		t.Errorf("log entry = %+v", entry)
	}
}

func TestReadyz(t *testing.T) {
	health := &httpapi.Health{}
	routes := (&httpapi.API{Health: health}).Routes()
	get := func() int {
		rec := httptest.NewRecorder()
		routes.ServeHTTP(rec, httptest.NewRequest(http.MethodGet, "/readyz", nil))
		return rec.Code
	}
	if code := get(); code != http.StatusOK {
		t.Fatalf("before Drain: %d, want 200", code)
	}
	health.Drain()
	if code := get(); code != http.StatusServiceUnavailable {
		t.Fatalf("after Drain: %d, want 503", code)
	}
}
