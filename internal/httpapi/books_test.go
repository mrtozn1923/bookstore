package httpapi_test

import (
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"os"
	"strings"
	"testing"

	"bookstore/internal/catalog"
	"bookstore/internal/httpapi"
	"bookstore/internal/orders"
	"bookstore/internal/orders/memstore"
)

func newAPI() *httpapi.API {
	books := catalog.Store{}
	return &httpapi.API{Books: books, Orders: orders.NewService(memstore.New(), books)}
}

func TestGetBook(t *testing.T) {
	routes := newAPI().Routes() // the mux fills r.PathValue("isbn")
	tests := []struct {
		path       string
		wantStatus int
		wantBody   string
	}{
		{"/api/books/9780441172719", http.StatusOK, `"title":"Dune"`},
		{"/api/books/9780000000000", http.StatusNotFound, "book not found"},
		{"/api/books/", http.StatusNotFound, "404 page not found"},
	}
	for _, tt := range tests {
		t.Run(tt.path, func(t *testing.T) {
			rec := httptest.NewRecorder()
			routes.ServeHTTP(rec, httptest.NewRequest(http.MethodGet, tt.path, nil))
			if rec.Code != tt.wantStatus || !strings.Contains(rec.Body.String(), tt.wantBody) {
				t.Errorf("GET %s = %d %q, want %d containing %q", tt.path, rec.Code, rec.Body, tt.wantStatus, tt.wantBody)
			}
		})
	}
}

func TestPlaceOrderEndToEnd(t *testing.T) {
	srv := httptest.NewServer(httpapi.Chain(newAPI().Routes(), httpapi.RequestID))
	defer srv.Close()

	body, err := os.Open("testdata/place_order.json") // go test runs in the package folder
	if err != nil {
		t.Fatal(err)
	}
	defer body.Close()
	resp, err := srv.Client().Post(srv.URL+"/api/orders", "application/json", body)
	if err != nil {
		t.Fatal(err)
	}
	defer resp.Body.Close()

	var got struct {
		ID    string `json:"id"`
		Total int64  `json:"total"`
	}
	if err := json.NewDecoder(resp.Body).Decode(&got); err != nil {
		t.Fatal(err)
	}
	if resp.StatusCode != http.StatusCreated || got.ID != "ORD-1001" || got.Total != 69980 {
		t.Errorf("got %d %+v, want 201 ORD-1001 total 69980", resp.StatusCode, got)
	}
	if loc := resp.Header.Get("Location"); loc != "/api/orders/ORD-1001" {
		t.Errorf("Location = %q", loc)
	}
	if resp.Header.Get("X-Request-ID") == "" {
		t.Error("X-Request-ID header is missing")
	}
}
