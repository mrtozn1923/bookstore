package orders

import (
	"context"
	"fmt"
	"time"
)

// Repository is what Service needs from storage. It is declared here, next
// to its consumer, and implemented elsewhere without naming it.
type Repository interface {
	Save(ctx context.Context, o *Order) error
	ByID(ctx context.Context, id string) (*Order, error)
}

// Service places orders.
type Service struct {
	repo   Repository
	now    func() time.Time
	nextID int
}

// NewService accepts any Repository and returns a concrete *Service.
func NewService(repo Repository) *Service {
	return &Service{repo: repo, now: time.Now, nextID: 1000}
}

// Place creates an order with the given items and saves it.
func (s *Service) Place(ctx context.Context, customerID string, items []Item) (*Order, error) {
	s.nextID++ // not safe for concurrent use yet; fixed in 08
	o := NewOrder(fmt.Sprintf("ORD-%d", s.nextID), customerID, s.now())
	for _, it := range items {
		o.AddItem(it, s.now())
	}
	if err := s.repo.Save(ctx, o); err != nil {
		return nil, err
	}
	return o, nil
}
