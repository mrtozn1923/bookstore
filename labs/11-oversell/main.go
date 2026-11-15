// Command 11-oversell sends 20 orders for the last 3 copies of Persuasion at
// the same time. Run with "naive" to see a read-then-write stock update lose
// updates; without arguments it uses sqlstore's conditional UPDATE.
package main

import (
	"context"
	"database/sql"
	"errors"
	"fmt"
	"log"
	"os"
	"sync"
	"sync/atomic"
	"time"

	_ "github.com/microsoft/go-mssqldb"

	"bookstore/internal/catalog"
	"bookstore/internal/orders"
	"bookstore/internal/orders/sqlstore"
)

const persuasion = "9780141439686"

func main() {
	ctx := context.Background()
	db, err := sql.Open("sqlserver", os.Getenv("BOOKSTORE_DB"))
	if err != nil {
		log.Fatal(err)
	}
	defer db.Close()
	if err := sqlstore.EnsureSchema(ctx, db, catalog.Store{}.All()); err != nil {
		log.Fatal(err)
	}
	if _, err := db.ExecContext(ctx, `UPDATE dbo.Books SET Stock = 3 WHERE Isbn = @p1`, persuasion); err != nil {
		log.Fatal(err)
	}

	// Open 20 connections up front, so the 20 orders really overlap.
	db.SetMaxIdleConns(20)
	var warm sync.WaitGroup
	for range 20 {
		warm.Go(func() {
			if _, err := db.ExecContext(ctx, `WAITFOR DELAY '00:00:00.200'`); err != nil {
				log.Fatal(err)
			}
		})
	}
	warm.Wait()

	place := placeWithService(db)
	if len(os.Args) > 1 && os.Args[1] == "naive" {
		place = placeNaive(db)
	}

	var placed, outOfStock, failed atomic.Int32
	var wg sync.WaitGroup
	for i := range 20 {
		wg.Go(func() {
			err := place(ctx, fmt.Sprintf("customer-%02d", i))
			switch {
			case err == nil:
				placed.Add(1)
			case errors.Is(err, orders.ErrOutOfStock):
				outOfStock.Add(1)
			default:
				failed.Add(1)
				log.Print(err)
			}
		})
	}
	wg.Wait()

	var stock int
	if err := db.QueryRowContext(ctx, `SELECT Stock FROM dbo.Books WHERE Isbn = @p1`, persuasion).Scan(&stock); err != nil {
		log.Fatal(err)
	}
	fmt.Printf("placed %d, out of stock %d, failed %d, stock left %d\n",
		placed.Load(), outOfStock.Load(), failed.Load(), stock)
}

// placeWithService is the real path: orders.Service + sqlstore.
func placeWithService(db *sql.DB) func(context.Context, string) error {
	svc := orders.NewService(sqlstore.New(db), catalog.Store{})
	return func(ctx context.Context, customer string) error {
		_, err := svc.Place(ctx, customer, []orders.Line{{ISBN: persuasion, Quantity: 1}})
		return err
	}
}

// placeNaive reads the stock, checks it in Go and writes the new value back.
// Two transactions can read the same value, and the later write wins.
func placeNaive(db *sql.DB) func(context.Context, string) error {
	return func(ctx context.Context, _ string) error {
		tx, err := db.BeginTx(ctx, nil)
		if err != nil {
			return err
		}
		defer tx.Rollback()
		var stock int
		if err := tx.QueryRowContext(ctx, `SELECT Stock FROM dbo.Books WHERE Isbn = @p1`, persuasion).Scan(&stock); err != nil {
			return err
		}
		if stock < 1 {
			return orders.ErrOutOfStock
		}
		time.Sleep(20 * time.Millisecond) // e.g. a call to the payment service
		if _, err := tx.ExecContext(ctx, `UPDATE dbo.Books SET Stock = @p1 WHERE Isbn = @p2`, stock-1, persuasion); err != nil {
			return err
		}
		return tx.Commit()
	}
}
