// Package memstore keeps orders in memory.
package memstore

import (
	"context"
	"fmt"

	"bookstore/internal/orders"
)

// Store is an in-memory order store. Not safe for concurrent use yet.
type Store struct {
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
	s.byID[o.ID] = o
	return nil
}

// ByID returns the order with the given ID.
func (s *Store) ByID(_ context.Context, id string) (*orders.Order, error) {
	o, ok := s.byID[id]
	if !ok {
		return nil, fmt.Errorf("id %s: %w", id, orders.ErrNotFound)
	}
	return o, nil
}
