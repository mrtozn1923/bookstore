// Package memstore keeps orders in memory.
package memstore

import (
	"context"
	"fmt"
	"sync"

	"bookstore/internal/orders"
)

// Store is an in-memory order store, safe for concurrent use.
type Store struct {
	mu   sync.RWMutex // guards byID
	byID map[string]*orders.Order
}

// Compile-time check that *Store satisfies orders.Repository.
var _ orders.Repository = (*Store)(nil)

// New returns an empty store.
func New() *Store {
	return &Store{byID: make(map[string]*orders.Order)}
}

// Save stores the order, replacing any order with the same ID.
func (s *Store) Save(_ context.Context, o *orders.Order) error {
	s.mu.Lock()
	defer s.mu.Unlock()
	s.byID[o.ID] = o
	return nil
}

// ByID returns the order with the given ID.
func (s *Store) ByID(_ context.Context, id string) (*orders.Order, error) {
	s.mu.RLock()
	defer s.mu.RUnlock()
	o, ok := s.byID[id]
	if !ok {
		return nil, fmt.Errorf("id %s: %w", id, orders.ErrNotFound)
	}
	return o, nil
}
