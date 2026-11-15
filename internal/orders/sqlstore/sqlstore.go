// Package sqlstore keeps orders in SQL Server through database/sql.
// The driver and the real queries arrive in post 11.
package sqlstore

import (
	"context"
	"database/sql"

	"bookstore/internal/orders"
)

// Store reads and writes the Orders table.
type Store struct {
	db *sql.DB
}

var _ orders.Repository = (*Store)(nil)

// New wraps an open *sql.DB.
func New(db *sql.DB) *Store {
	return &Store{db: db}
}

// Save inserts the order header. Items and transactions come in post 11.
func (s *Store) Save(ctx context.Context, o *orders.Order) error {
	_, err := s.db.ExecContext(ctx,
		`INSERT INTO Orders (Id, CustomerId, Status, CreatedAt) VALUES (@p1, @p2, @p3, @p4)`,
		o.ID, o.CustomerID, int(o.Status), o.CreatedAt)
	return err
}

// ByID loads the order header.
func (s *Store) ByID(ctx context.Context, id string) (*orders.Order, error) {
	o := &orders.Order{}
	err := s.db.QueryRowContext(ctx,
		`SELECT Id, CustomerId, Status, CreatedAt FROM Orders WHERE Id = @p1`, id).
		Scan(&o.ID, &o.CustomerID, &o.Status, &o.CreatedAt)
	if err != nil {
		return nil, err
	}
	return o, nil
}
