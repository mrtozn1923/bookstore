package orders

import (
	"context"
	"errors"
	"fmt"
	"testing"
	"time"

	"bookstore/internal/catalog"
)

// fakeRepo is a hand-written Repository for tests: no mock library, just a
// struct that remembers what it was given.
type fakeRepo struct {
	saved   []*Order
	saveErr error
}

func (f *fakeRepo) NextID(context.Context) (string, error) {
	return fmt.Sprintf("ORD-%d", 1001+len(f.saved)), nil
}

func (f *fakeRepo) Save(_ context.Context, o *Order) error {
	if f.saveErr != nil {
		return f.saveErr
	}
	f.saved = append(f.saved, o)
	return nil
}

func (f *fakeRepo) ByID(context.Context, string) (*Order, error) {
	return nil, ErrNotFound
}

// fakeCatalog serves a fixed set of books.
type fakeCatalog map[string]catalog.Book

func (c fakeCatalog) ByISBN(isbn string) (catalog.Book, error) {
	b, ok := c[isbn]
	if !ok {
		return catalog.Book{}, fmt.Errorf("isbn %s: %w", isbn, catalog.ErrBookNotFound)
	}
	return b, nil
}

var testBooks = fakeCatalog{
	"9780441172719": {ISBN: "9780441172719", Title: "Dune", Price: 34990, Stock: 2},
	"9780547928227": {ISBN: "9780547928227", Title: "The Hobbit", Price: 28950, Stock: 0},
}

// newTestService returns a Service with a fake store and a fixed clock.
func newTestService(t *testing.T, repo *fakeRepo) *Service {
	t.Helper()
	s := NewService(repo, testBooks)
	s.now = func() time.Time { return time.Date(2026, 11, 15, 9, 30, 0, 0, time.UTC) }
	return s
}

func TestPlaceStockRule(t *testing.T) {
	tests := []struct {
		name    string
		isbn    string
		qty     int
		wantErr error // nil: the order must be placed
	}{
		{"less than stock", "9780441172719", 1, nil},
		{"exactly the stock", "9780441172719", 2, nil},
		{"one more than stock", "9780441172719", 3, ErrOutOfStock},
		{"sold out", "9780547928227", 1, ErrOutOfStock},
		{"unknown isbn", "9780000000000", 1, catalog.ErrBookNotFound},
	}
	for _, tt := range tests {
		t.Run(tt.name, func(t *testing.T) {
			t.Parallel()
			repo := &fakeRepo{}
			_, err := newTestService(t, repo).Place(t.Context(), "ada", []Line{{ISBN: tt.isbn, Quantity: tt.qty}})
			if !errors.Is(err, tt.wantErr) {
				t.Fatalf("Place(%s x%d) error = %v, want %v", tt.isbn, tt.qty, err, tt.wantErr)
			}
			if wantSaved := tt.wantErr == nil; (len(repo.saved) == 1) != wantSaved {
				t.Errorf("saved %d orders, want saved = %v", len(repo.saved), wantSaved)
			}
		})
	}
}

func TestPlaceUsesClockAndStoreID(t *testing.T) {
	repo := &fakeRepo{}
	o, err := newTestService(t, repo).Place(t.Context(), "ada", []Line{{ISBN: "9780441172719", Quantity: 2}})
	if err != nil {
		t.Fatal(err)
	}
	want := time.Date(2026, 11, 15, 9, 30, 0, 0, time.UTC)
	if o.ID != "ORD-1001" || !o.CreatedAt.Equal(want) || o.Total() != 69980 {
		t.Errorf("got %s created %v total %v; want ORD-1001 created %v total 699.80 TRY", o.ID, o.CreatedAt, o.Total(), want)
	}
}

func TestPlaceValidation(t *testing.T) {
	_, err := newTestService(t, &fakeRepo{}).Place(t.Context(), "", []Line{{ISBN: "9780441172719", Quantity: 0}})
	got := ValidationErrors(err)
	if len(got) != 2 || got[0].Field != "customerId" || got[1].Field != "items[0].quantity" {
		t.Fatalf("ValidationErrors = %v, want customerId and items[0].quantity", got)
	}
}

func TestPlaceSaveError(t *testing.T) {
	boom := errors.New("connection reset")
	_, err := newTestService(t, &fakeRepo{saveErr: boom}).Place(t.Context(), "ada", []Line{{ISBN: "9780441172719", Quantity: 1}})
	if !errors.Is(err, boom) {
		t.Fatalf("error = %v, want it to wrap %v", err, boom)
	}
}
