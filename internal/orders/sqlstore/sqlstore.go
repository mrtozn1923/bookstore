// Package sqlstore keeps orders in SQL Server through database/sql and the
// go-mssqldb driver.
package sqlstore

import (
	"cmp"
	"context"
	"database/sql"
	_ "embed"
	"errors"
	"fmt"
	"slices"
	"time"

	"bookstore/internal/catalog"
	"bookstore/internal/orders"
)

//go:embed schema.sql
var schema string

// Store reads and writes the Orders and OrderItems tables and keeps
// Books.Stock in step with every saved order.
type Store struct {
	db *sql.DB
}

var _ orders.Repository = (*Store)(nil)

// New wraps an open *sql.DB. The caller owns db and closes it.
func New(db *sql.DB) *Store {
	return &Store{db: db}
}

// EnsureSchema creates the tables that do not exist yet and inserts the
// books that are missing. Real projects use a migration tool instead.
func EnsureSchema(ctx context.Context, db *sql.DB, books []catalog.Book) error {
	if _, err := db.ExecContext(ctx, schema); err != nil {
		return fmt.Errorf("create schema: %w", err)
	}
	for _, b := range books {
		_, err := db.ExecContext(ctx, `
			IF NOT EXISTS (SELECT 1 FROM dbo.Books WHERE Isbn = @isbn)
			INSERT INTO dbo.Books (Isbn, Title, Author, Price, Stock)
			VALUES (@isbn, @title, @author, @price, @stock)`,
			sql.Named("isbn", b.ISBN), sql.Named("title", b.Title), sql.Named("author", b.Author),
			sql.Named("price", b.Price), sql.Named("stock", b.Stock))
		if err != nil {
			return fmt.Errorf("seed book %s: %w", b.ISBN, err)
		}
	}
	return nil
}

// NextID takes the next number from the OrderNumbers sequence.
func (s *Store) NextID(ctx context.Context) (string, error) {
	var n int
	if err := s.db.QueryRowContext(ctx, `SELECT NEXT VALUE FOR dbo.OrderNumbers`).Scan(&n); err != nil {
		return "", fmt.Errorf("next order id: %w", err)
	}
	return fmt.Sprintf("ORD-%d", n), nil
}

// Save inserts the order and its items and takes the quantities out of
// stock, all in one transaction: either everything is stored or nothing.
func (s *Store) Save(ctx context.Context, o *orders.Order) error {
	tx, err := s.db.BeginTx(ctx, nil)
	if err != nil {
		return fmt.Errorf("begin: %w", err)
	}
	defer tx.Rollback() // after Commit this returns sql.ErrTxDone and does nothing

	_, err = tx.ExecContext(ctx, `
		INSERT INTO dbo.Orders (Id, CustomerId, Status, CreatedAt, UpdatedAt, PaidAt)
		VALUES (@id, @customer, @status, @created, @updated, @paid)`,
		sql.Named("id", o.ID), sql.Named("customer", o.CustomerID), sql.Named("status", int(o.Status)),
		sql.Named("created", o.CreatedAt.UTC()), sql.Named("updated", o.UpdatedAt.UTC()), sql.Named("paid", utc(o.PaidAt)))
	if err != nil {
		return fmt.Errorf("insert order: %w", err)
	}

	// Update stock in ISBN order, so two orders for the same books always
	// lock the rows in the same order and cannot deadlock each other.
	byISBN := slices.SortedFunc(slices.Values(o.Items), func(a, b orders.Item) int {
		return cmp.Compare(a.ISBN, b.ISBN)
	})
	for _, it := range byISBN {
		res, err := tx.ExecContext(ctx, `
			UPDATE dbo.Books SET Stock = Stock - @qty
			WHERE Isbn = @isbn AND Stock >= @qty`,
			sql.Named("qty", it.Quantity), sql.Named("isbn", it.ISBN))
		if err != nil {
			return fmt.Errorf("take %s from stock: %w", it.ISBN, err)
		}
		if n, err := res.RowsAffected(); err != nil {
			return fmt.Errorf("take %s from stock: %w", it.ISBN, err)
		} else if n == 0 {
			return fmt.Errorf("isbn %s: %d requested: %w", it.ISBN, it.Quantity, orders.ErrOutOfStock)
		}
	}

	for i, it := range o.Items {
		_, err := tx.ExecContext(ctx, `
			INSERT INTO dbo.OrderItems (OrderId, LineNumber, Isbn, Title, UnitPrice, Quantity)
			VALUES (@order, @line, @isbn, @title, @price, @qty)`,
			sql.Named("order", o.ID), sql.Named("line", i+1), sql.Named("isbn", it.ISBN),
			sql.Named("title", it.Title), sql.Named("price", it.UnitPrice), sql.Named("qty", it.Quantity))
		if err != nil {
			return fmt.Errorf("insert item %d: %w", i+1, err)
		}
	}
	return tx.Commit()
}

// utc converts t to UTC. The driver sends a time.Time as datetimeoffset;
// SQL Server drops the offset when it stores the value in a datetime2
// column, so a local time would be stored as if it were UTC.
func utc(t *time.Time) *time.Time {
	if t == nil {
		return nil
	}
	u := t.UTC()
	return &u
}

// ByID loads an order and its items. Every call builds a new *Order, so
// two requests never share one value.
func (s *Store) ByID(ctx context.Context, id string) (*orders.Order, error) {
	o := &orders.Order{}
	err := s.db.QueryRowContext(ctx, `
		SELECT Id, CustomerId, Status, CreatedAt, UpdatedAt, PaidAt
		FROM dbo.Orders WHERE Id = @id`, sql.Named("id", id)).
		Scan(&o.ID, &o.CustomerID, &o.Status, &o.CreatedAt, &o.UpdatedAt, &o.PaidAt)
	if errors.Is(err, sql.ErrNoRows) {
		return nil, fmt.Errorf("id %s: %w", id, orders.ErrNotFound)
	}
	if err != nil {
		return nil, fmt.Errorf("load order %s: %w", id, err)
	}

	rows, err := s.db.QueryContext(ctx, `
		SELECT Isbn, Title, UnitPrice, Quantity
		FROM dbo.OrderItems WHERE OrderId = @id ORDER BY LineNumber`, sql.Named("id", id))
	if err != nil {
		return nil, fmt.Errorf("load items of %s: %w", id, err)
	}
	defer rows.Close() // gives the connection back to the pool
	for rows.Next() {
		var it orders.Item
		if err := rows.Scan(&it.ISBN, &it.Title, &it.UnitPrice, &it.Quantity); err != nil {
			return nil, fmt.Errorf("scan item of %s: %w", id, err)
		}
		o.Items = append(o.Items, it)
	}
	if err := rows.Err(); err != nil { // an error that ended the loop early
		return nil, fmt.Errorf("read items of %s: %w", id, err)
	}
	return o, nil
}
