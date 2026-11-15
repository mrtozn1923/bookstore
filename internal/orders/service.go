package orders

import (
	"context"
	"errors"
	"fmt"
	"sync"
	"time"

	"bookstore/internal/catalog"
)

// Repository is what Service needs from storage. It is declared here, next
// to its consumer, and implemented elsewhere without naming it.
type Repository interface {
	Save(ctx context.Context, o *Order) error
	ByID(ctx context.Context, id string) (*Order, error)
}

// Catalog is what Service needs from the catalog.
type Catalog interface {
	ByISBN(isbn string) (catalog.Book, error)
}

// Line is one requested book in a new order.
type Line struct {
	ISBN     string
	Quantity int
}

// Service places orders. It is safe for concurrent use.
type Service struct {
	repo  Repository
	books Catalog
	now   func() time.Time

	mu     sync.Mutex // guards nextID
	nextID int
}

// NewService accepts interfaces and returns a concrete *Service.
func NewService(repo Repository, books Catalog) *Service {
	return &Service{repo: repo, books: books, now: time.Now, nextID: 1000}
}

// Place validates the request, checks stock and saves a new order.
func (s *Service) Place(ctx context.Context, customerID string, lines []Line) (*Order, error) {
	if err := validate(customerID, lines); err != nil {
		return nil, err
	}
	items := make([]Item, 0, len(lines))
	for _, l := range lines {
		b, err := s.books.ByISBN(l.ISBN)
		if err != nil {
			return nil, fmt.Errorf("place order: %w", err)
		}
		if b.Stock < l.Quantity {
			return nil, fmt.Errorf("place order: %q has %d in stock, %d requested: %w",
				b.Title, b.Stock, l.Quantity, ErrOutOfStock)
		}
		items = append(items, Item{ISBN: b.ISBN, Title: b.Title, UnitPrice: b.Price, Quantity: l.Quantity})
	}

	now := s.now()
	o := NewOrder(s.newID(), customerID, now)
	for _, it := range items {
		o.AddItem(it, now)
	}
	if err := s.repo.Save(ctx, o); err != nil {
		return nil, fmt.Errorf("save order %s: %w", o.ID, err)
	}
	return o, nil
}

// newID returns the next order ID. Only the counter is locked, not the
// catalog lookups or the repository call.
func (s *Service) newID() string {
	s.mu.Lock()
	defer s.mu.Unlock()
	s.nextID++
	return fmt.Sprintf("ORD-%d", s.nextID)
}

// validate collects every problem instead of stopping at the first one.
func validate(customerID string, lines []Line) error {
	var errs []error
	if customerID == "" {
		errs = append(errs, ValidationError{Field: "customerId", Message: "is required"})
	}
	if len(lines) == 0 {
		errs = append(errs, ValidationError{Field: "items", Message: "must not be empty"})
	}
	for i, l := range lines {
		if l.Quantity <= 0 {
			errs = append(errs, ValidationError{Field: fmt.Sprintf("items[%d].quantity", i), Message: "must be positive"})
		}
	}
	return errors.Join(errs...) // nil when errs is empty
}
